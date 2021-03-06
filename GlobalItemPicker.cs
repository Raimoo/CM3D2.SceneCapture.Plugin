using System;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityInjector;
using UnityInjector.Attributes;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace CM3D2.SceneCapture.Plugin
{
    public static class GlobalItemPicker
    {
        private static List<MenuInfo> Menus = new List<MenuInfo>();

        static GlobalItemPicker() {
            itemPicker = new ItemPickerWindow(300);
            gsWin = new GUIStyle("box");
            gsWin.fontSize = Util.GetPix(12);
            gsWin.alignment = TextAnchor.UpperRight;
        }

        public static void Update()
        {
            if(itemPicker.show)
            {
                itemPicker.rect = GUI.Window(itemPicker.WINDOW_ID, itemPicker.rect, itemPicker.GuiFunc, string.Empty, gsWin);
            }
        }

        public static bool Visible
        {
            get
            {
                return itemPicker.show;
            }
        }

        public static void SetMenus(Dictionary<MPN, List<MenuInfo>> menus)
        {
            itemPicker.Menus = menus;
        }

        public static bool MenusAreSet()
        {
            return itemPicker.Menus.Any();
        }

        public static void Set(Vector2 p, float fWidth, int iFontSize, Action<MenuInfo> f)
        {
            itemPicker.Set(p, fWidth, iFontSize, f);
        }

        private static GUIStyle gsWin;
        private static ItemPickerWindow itemPicker;

        internal class ItemPickerWindow
        {
            public readonly int WINDOW_ID;

            public Rect rect { get; set; }
            private float fWidth { get; set; }
            private float fMargin { get; set; }
            private float fRightPos { get; set; }
            private float fUpPos { get; set; }
            private float guiScrollHeight { get; set; }

            public bool show { get; private set; }

            public Action<MenuInfo> func { get; private set; }

            private GUIStyle gsLabel { get; set; }
            private GUIStyle gsButton { get; set; }

            private Vector2 scrollPosition { get; set; }

            private List<CustomTextureButton> buttons = null;
            private CustomComboBox categoryBox = null;
            private Dictionary<MPN, List<MenuInfo>> _menus;
            public Dictionary<MPN, List<MenuInfo>> Menus {
                get
                {
                    return this._menus;
                }
                set
                {
                    this._menus = value;
                }
            }

            public ItemPickerWindow(int iWIndowID)
            {
                WINDOW_ID = iWIndowID;
                buttons = new List<CustomTextureButton>();

                this.categoryBox = new CustomComboBox( ConstantValues.PropParts.Keys.ToArray() );
                this.categoryBox.SelectedIndex = 0;
                this.categoryBox.SelectedIndexChanged += this.ChangeCategory;

                Menus = new Dictionary<MPN, List<MenuInfo>>();
            }

            public void ChangeCategory( object sender, EventArgs args )
            {
                UpdateMenus(ConstantValues.PropParts[ categoryBox.SelectedItem ]);
            }

            public void UpdateMenus(MPN category)
            {
                buttons.Clear();
                List<MenuInfo> existing;
                if(!Menus.TryGetValue(category, out existing))
                    return;

                foreach(MenuInfo menu in existing)
                {
                    try{
                        if(menu != null)
                        {
                            if(menu.delOnly)
                                continue;

                            if(menu.partCategory == category)
                            {
                                Texture2D iconTexture = AssetLoader.LoadTexture( menu.iconTextureName );
                                if( iconTexture == null )
                                {
                                    iconTexture = new Texture2D(1, 1);
                                    iconTexture.SetPixel(0, 0, new Color32(1, 1, 1, 1));
                                    iconTexture.Apply();
                                }
                                CustomTextureButton button = new CustomTextureButton( iconTexture );
                                if( menu.modelName == null )
                                    continue;
                                string modelName = String.Copy(menu.modelName);

                                button.Text = modelName;

                                string modelIconName;
                                if(menu.iconTextureName != null)
                                    modelIconName = String.Copy(menu.iconTextureName);
                                else
                                    modelIconName = "";

                                MenuInfo menuCopy = new MenuInfo(menu);
                                button.Click += (o, e) => func(menuCopy);
                                buttons.Add(button);
                            }
                        }
                        else
                        {
                            Debug.LogError("Null MenuInfo");
                        }
                    }
                    catch
                    {
                        // Debug.LogError( e );
                    }
                }
            }

            public void UpdateMenusBGObject()
            {
                buttons.Clear();

                if(PhotoBGObjectData.data == null)
                    PhotoBGObjectData.Create();

                foreach(PhotoBGObjectData data in PhotoBGObjectData.data)
                {
                    Texture2D iconTexture = new Texture2D(1, 1);
                    iconTexture.SetPixel(0, 0, new Color32(1, 1, 1, 1));
                    iconTexture.Apply();
                    CustomTextureButton button = new CustomTextureButton( iconTexture );
                    string modelName = String.Copy(data.name);

                    button.Text = modelName;

                    MenuInfo menuCopy = MenuInfo.MakeBGObjectMenu(data);
                    button.Click += (o, e) => func(menuCopy);
                    buttons.Add(button);
                }
            }

            public void UpdateMenusBackground()
            {
                buttons.Clear();

                foreach(var bg in ConstantValues.Background.Skip(2))
                {
                    Texture2D iconTexture = new Texture2D(1, 1);
                    iconTexture.SetPixel(0, 0, new Color32(1, 1, 1, 1));
                    iconTexture.Apply();
                    CustomTextureButton button = new CustomTextureButton( iconTexture );
                    string modelName = String.Copy(bg.Key);

                    button.Text = modelName;

                    MenuInfo menuCopy = MenuInfo.MakeBackgroundMenu(bg.Value);
                    button.Click += (o, e) => func(menuCopy);
                    buttons.Add(button);
                }
            }

            public void Set(Vector2 p, float fWidth, int iFontSize, Action<MenuInfo> f)
            {
                rect = new Rect(p.x - fWidth, p.y, fWidth, 0f);
                fRightPos = p.x + fWidth;
                fUpPos = p.y;
                this.fWidth = fWidth;

                gsLabel = new GUIStyle("label");
                gsLabel.fontSize = iFontSize;
                gsLabel.alignment = TextAnchor.MiddleLeft;

                gsButton = new GUIStyle("button");
                gsButton.fontSize = iFontSize;
                gsButton.alignment = TextAnchor.MiddleCenter;

                fMargin = iFontSize * 0.3f;
                scrollPosition = new Vector2(0, 0);

                func = f;

                show = true;
            }

            private void categoryButton(string value, Rect rectItem)
            {
                if(GUI.Button(rectItem, Translation.GetText("Equip", value), gsButton))
                {
                    UpdateMenus(ConstantValues.PropParts[value]);
                }
            }

            private void backgroundObjectButton(Rect rectItem)
            {
                if(GUI.Button(rectItem, Translation.GetText("Equip", "backgroundObject"), gsButton))
                {
                    UpdateMenusBGObject();
                }
            }

            private void backgroundButton(Rect rectItem)
            {
                if(GUI.Button(rectItem, Translation.GetText("Equip", "background"), gsButton))
                {
                    UpdateMenusBackground();
                }
            }

            public void GuiFunc(int winId)
            {
                float windowHeight = this.rect.height;
                if( this.rect.height > Screen.height * 0.7f )
                {
                    windowHeight = Screen.height * 0.7f;
                }

                int iFontSize = gsLabel.fontSize;
                Rect rectScroll = new Rect(0, fMargin * 2, this.rect.width - fMargin, windowHeight - fMargin * 4);
                Rect rectScrollView = new Rect(0, 0, this.rect.width - 5 * fMargin, guiScrollHeight);

                scrollPosition = GUI.BeginScrollView(rectScroll, scrollPosition, rectScrollView, false, true);

                float buttonWidth = (fWidth - fMargin * 4 - iFontSize) / 5 ;
                float buttonHeight = iFontSize * 1.5f;
                string[] values = ConstantValues.PropParts.Keys.ToArray();

                Rect rectItem = new Rect(iFontSize * 0.5f, iFontSize * 0.5f, buttonWidth, buttonHeight);

                for(int i = 0; i < ConstantValues.PropParts.Count(); i++)
                {
                    categoryButton(values[i], rectItem);
                    if((i+1) % 5 == 0)
                    {
                        rectItem.x = iFontSize * 0.5f;
                        rectItem.y += buttonHeight;
                    }
                    else
                    {
                        rectItem.x += buttonWidth;
                    }
                }

                this.backgroundObjectButton(rectItem);

                rectItem.x = iFontSize * 0.5f;
                rectItem.y += buttonHeight;
                this.backgroundButton(rectItem);

                rectItem = new Rect(iFontSize * 0.5f, rectItem.y + buttonHeight + fMargin * 2, buttonWidth, buttonWidth);
                // GUI.DrawTexture(rectItem, texture);

                int j = 0;
                foreach(CustomTextureButton button in buttons)
                {
                    rectItem.x = iFontSize * 0.5f + (j * buttonWidth);
                    button.SetFromRect(rectItem);
                    button.OnGUI();

                    j += 1;
                    if(j == 5) {
                        j = 0;
                        rectItem.y += iFontSize * 5f;
                    }
                }
                GUI.EndScrollView();

                float fHeight = rectItem.y + rectItem.height + fMargin;
                if (fHeight > Screen.height * 0.7f) {
                    fHeight = Screen.height * 0.7f;
                }

                if (rect.height != fHeight)
                {
                    Rect rectTmp = new Rect(rect.x, rect.y - fHeight, rect.width, fHeight);
                    rect = rectTmp;
                }
                else if (rect.x < 0f)
                {
                    Rect rectTmp = new Rect(fRightPos, rect.y, rect.width, rect.height);
                    rect = rectTmp;
                }
                else if (rect.y < 0f)
                {
                    Rect rectTmp = new Rect(rect.x, fUpPos, rect.width, rect.height);
                    rect = rectTmp;
                }

                guiScrollHeight = rectItem.y + rectItem.height;

                {
                    Vector2 mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

                    bool enableGameGui = true;
                    bool m = Input.GetAxis("Mouse ScrollWheel") != 0;
                    for (int i = 0; i < 3; i++)
                    {
                        m |= Input.GetMouseButtonDown(i);
                    }
                    if (m)
                    {
                        enableGameGui = !rect.Contains(mousePos);
                    }
                    GameMain.Instance.MainCamera.SetControl(enableGameGui);
                    UICamera.InputEnable = enableGameGui;
                }

                GUI.DragWindow();

                // Vector2 screenSize = new Vector2(Screen.width, Screen.height);
                // if (guiScrollHeight > screenSize.y * 0.7f)
                // {
                //     rect = new Rect(rect.x, rect.y, rect.width, screenSize.y * 0.7f);
                // }
                // else
                // {
                //     rect = new Rect(rect.x, rect.y, rect.width, guiScrollHeight);
                // }

                if (GetAnyMouseButtonDown())
                {
                    Vector2 v2Tmp = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                    if (!rect.Contains(v2Tmp))
                    {
                        show = false;
                    }
                }
            }

            private bool GetAnyMouseButtonDown()
            {
                return Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
            }

        }
    }
}
