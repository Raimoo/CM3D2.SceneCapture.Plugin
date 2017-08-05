using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;
using UnityEngine;
using UnityInjector;
using UnityInjector.Attributes;

namespace CM3D2.SceneCapture.Plugin
{
    public class SerializeStatic
    {
        private static readonly Type[] ALLOWED_TYPES = { typeof(Vector3), typeof(Color), typeof(Color32), typeof(AnimationCurve), typeof(Transform) };

        private static bool TypeAllowed(Type t)
        {
            foreach(Type other in ALLOWED_TYPES)
            {
                if (other == t)
                {
                    return true;
                }
            }
            return false;
        }

        private static string GetValue(object instance, Type fieldType, FieldInfo field)
        {
            string result = null;
            object val = field.GetValue (instance);

            if (val == null)
                return null;

            if (fieldType == typeof(Shader) || fieldType == typeof(Texture2D))
                return null;

            if (fieldType.IsPrimitive)
            {
                result = val.ToString();
            }
            else if (fieldType.IsEnum)
            {
                result = ((int)val).ToString();
            }
            else if (fieldType == typeof(Vector3))
            {
                Vector3 v3 = (Vector3)val;
                result = Util.ConvertVector3ToString(v3);
            }
            else if (fieldType == typeof(Color) || fieldType == typeof(Color32))
            {
                Color32 color = (Color)val;
                result = Util.ConvertColor32ToString(color);
            }
            else if (fieldType == typeof(AnimationCurve))
            {
                AnimationCurve curve = (AnimationCurve)val;
                result = Util.ConvertAnimationCurveToString(curve);
            }
            else if (fieldType == typeof(Transform))
            {
                Vector3 v3 = ((Transform)val).position;
                result = Util.ConvertVector3ToString(v3);
            }

            return result;
        }

        public static XElement SaveDef(Type effectDefType, Type enabled_effect)
        {
            try
            {
                FieldInfo[] fields = enabled_effect.GetFields();
                string[][] a = new string[fields.Length][];
                int i = 0;


                PropertyInfo enabledProperty = enabled_effect.GetProperty("enabled");
                bool enabled = false;
                // else if(typeof(GlobalFogDef) == effectDefType)
                // {
                //     enabled = GlobalFogDef.globalFogEffect.enabled;
                // }
                FieldInfo effectField = Util.GetFieldsSpecifyType(effectDefType, enabled_effect)[0];
                object effect = effectField.GetValue(null);

                if(typeof(BloomDef) == effectDefType)
                {
                    Debug.Log("Enabled bloom: " + BloomDef.enabledInPane);
                    enabled = BloomDef.enabledInPane;
                }
                else if (enabledProperty != null)
                {
                    enabled = (bool)enabledProperty.GetValue(effect, null);
                }

                if (enabled)
                {
                    foreach (FieldInfo field in fields)
                    {
                        Type fieldType = field.FieldType;
                        Debug.Log("PropName: " + field.Name.ToString());
                        if(fieldType.IsPrimitive || fieldType.IsEnum || TypeAllowed(fieldType)) {
                            Debug.Log("get: " + field.Name.ToString());

                            string val;
                            if (typeof(Bloom) == enabled_effect && field.Name == "bloomIntensity")
                            {
                                Debug.Log("CM Bloom get " + GameMain.Instance.CMSystem.BloomValue.ToString());
                                val = GameMain.Instance.CMSystem.BloomValue.ToString();
                            }
                            else
                            {
                                val = GetValue(effect, fieldType, field);
                            }

                            if(val != null) {
                                a[i] = new string[2];
                                a[i][0] = field.Name;
                                a[i][1] = val;
                                i++;
                            }
                        }
                    }
                }
                else
                {
                    return new XElement("null");
                }

                var dlx = new XElement(effectDefType.Name);
                foreach(string[] field in a)
                {
                    if(field != null)
                    {
                        var elem = new XElement(field[0], field[1]);
                        dlx.Add(elem);
                    }
                }

                return dlx;
            }
            catch(Exception e)
            {
                Debug.LogError( "Save error: " +  e.ToString() );
                return null;
            }
        }

        public static void LoadDef(XElement allEffects, Type effectDefType, Type enabled_effect)
        {
            try
            {
                FieldInfo effectField = Util.GetFieldsSpecifyType(effectDefType, enabled_effect)[0];
                var effect = effectField.GetValue(null);

                XElement def = allEffects.Elements().Where(e => e.Name.ToString() == effectDefType.Name).FirstOrDefault();
                PropertyInfo enabledProperty;
                if(typeof(Bloom) == enabled_effect)
                {
                    enabledProperty = effectDefType.GetProperty("enabledInPane");
                }
                else
                {
                    enabledProperty = enabled_effect.GetProperty("enabled");
                }

                if(def == null)
                {
                    // Effect was not enabled in the preset. Disable it.
                    if(typeof(Bloom) == enabled_effect)
                    {
                        Debug.Log("Resetting bloom!");
                        enabledProperty.SetValue(null, false, null);
                        MethodInfo mi = effectDefType.GetMethod("Reset");
                        if (mi != null)
                            mi.Invoke(null, null);
                    }
                    else
                    {
                        enabledProperty.SetValue(effect, false, null);
                    }
                }
                else
                {
                    Debug.Log(def.ToString());
                    if(typeof(BloomDef) == enabled_effect)
                    {
                        Debug.Log("Enabling bloom!");
                        enabledProperty.SetValue(null, true, null);
                    }
                    else
                    {
                        enabledProperty.SetValue(effect, true, null);
                    }

                    foreach (XElement propElem in def.Elements())
                    {
                        string propName = propElem.Name.ToString();
                        FieldInfo field = enabled_effect.GetField(propName);
                        if(field == null)
                        {
                            Debug.LogError("Failed to load field! " + propName);
                            continue;
                        }

                        Type fieldType = field.FieldType;

                        int iTmp;
                        float fTmp;
                        bool bTmp;
                        float.TryParse(propElem.Value, out fTmp);

                        if (field != null)
                        {
                            if (typeof(Bloom) == enabled_effect && field.Name == "bloomIntensity")
                            {
                                Debug.Log("CM bloom " + propElem.Value);
                                if (int.TryParse(propElem.Value, out iTmp))
                                    GameMain.Instance.CMSystem.BloomValue = iTmp;
                            }
                            else if (fieldType == typeof(int) || fieldType.IsEnum)
                            {
                                if (int.TryParse(propElem.Value, out iTmp))
                                    field.SetValue(effect, iTmp);
                            }
                            else if (fieldType == typeof(float))
                            {
                                if (float.TryParse(propElem.Value, out fTmp))
                                    field.SetValue(effect, fTmp);
                            }
                            else if (fieldType == typeof(bool))
                            {
                                if (bool.TryParse(propElem.Value, out bTmp))
                                    field.SetValue(effect, bTmp);
                            }
                            else if (fieldType == typeof(Vector3))
                            {
                                Vector3 v3 = Util.ConvertStringToVector3(propElem.Value);
                                field.SetValue(effect, v3);
                            }
                            else if (fieldType == typeof(Color) || fieldType == typeof(Color32))
                            {
                                Color color = Util.ConvertStringToColor32(propElem.Value);
                                field.SetValue(effect, color);
                            }
                            else if (fieldType == typeof(AnimationCurve))
                            {
                                AnimationCurve curve = Util.ConvertStringToAnimationCurve(propElem.Value);
                                field.SetValue(effect, curve);
                            }
                            else if (fieldType == typeof(Transform))
                            {
                                Vector3 v3 = Util.ConvertStringToVector3(propElem.Value);
                                ((Transform)field.GetValue(effect)).position = v3;
                            }
                        }
                    };
                }

                // MethodInfo mi = effectDefType.GetMethod("InitMemberByInstance");
                // if (mi != null)
                //     mi.Invoke(null, new object[] { effect });
                // else
                //     Debug.LogWarning("Couldn't init " + effectDefType + "!!!");

                // MethodInfo mi = effectDefType.GetMethod("Reset");
                // if (mi != null)
                //     mi.Invoke(null, null);
                // else
                //     Debug.LogWarning("Couldn't reset " + effectDefType + "!!!");
            }
            catch(Exception e)
            {
                Debug.LogError( "Load error: " +  e.ToString() );
            }
        }

    }
}