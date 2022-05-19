using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ET;
using FairyGUI.Utils;
using UnityEngine;

namespace FUIEditor
{
    public static class FUICodeSpawner
    {
        private const string NameSpace = "ET";
        
        private const string ClassNamePrefix = "FUI_";
        
        private const string SpawnCodeDir = "../Unity/Codes/ModelView/Demo/FUIAutoGen/";
        
        private static readonly Dictionary<string, PackageInfo> PackageInfos = new Dictionary<string, PackageInfo>();

        private static readonly Dictionary<string, ComponentInfo> ComponentInfos = new Dictionary<string, ComponentInfo>();
        
        private static readonly MultiDictionary<string, string, ComponentInfo> ExportedComponentInfos = new MultiDictionary<string, string, ComponentInfo>();

        private static readonly HashSet<string> ExtralExportURLs = new HashSet<string>();

        private static readonly Dictionary<ObjectType, string> ObjectTypeToClassType = new Dictionary<ObjectType, string>()
        {
            {ObjectType.graph, "GGraph"},
            {ObjectType.group, "GGroup"},
            {ObjectType.image, "GImage"},
            {ObjectType.loader, "GLoader"},
            {ObjectType.loader3D, "GLoader3D"},
            {ObjectType.movieclip, "GMovieClip"},
            {ObjectType.textfield, "GTextField"},
            {ObjectType.textinput, "GTextInput"},
            {ObjectType.richtext, "GRichTextField"},
            {ObjectType.list, "GList"}
        };
        
        private static readonly Dictionary<ComponentType, string> ComponentTypeToClassType = new Dictionary<ComponentType, string>()
        {
            {ComponentType.Component, "GComponent"},
            {ComponentType.Button, "GButton"},
            {ComponentType.ComboBox, "GComboBox"},
            {ComponentType.Label, "GLabel"},
            {ComponentType.ProgressBar, "GProgressBar"},
            {ComponentType.ScrollBar, "GScrollBar"},
            {ComponentType.Slider, "GSlider"},
            {ComponentType.Tree, "GTree"}
        };

        public static void ParseAndSpawnCode()
        {
            ParseAllPackages();
            GenCode();
        }

        private static void ParseAllPackages()
        {
            PackageInfos.Clear();
            ComponentInfos.Clear();
            ExportedComponentInfos.Clear();
            ExtralExportURLs.Clear();

            string fuiAssetsDir = Application.dataPath + "/../../FGUIProject/assets";
            string[] packageDirs = Directory.GetDirectories(fuiAssetsDir);
            foreach (var packageDir in packageDirs)
            {
                PackageInfo packageInfo = ParsePackage(packageDir);
                PackageInfos.Add(packageInfo.Id, packageInfo);
            }
        }

        private static PackageInfo ParsePackage(string packageDir)
        {
            PackageInfo packageInfo = new PackageInfo();

            packageInfo.Path = packageDir;
            packageInfo.Name = Path.GetFileName(packageDir);
                
            XML xml = new XML(File.ReadAllText(packageDir + "/package.xml"));
            packageInfo.Id = xml.GetAttribute("id");

            if (xml.elements[0].name != "resources" || xml.elements[1].name != "publish")
            {
                throw new Exception("package.xml 格式不对！");
            }
            
            foreach (XML element in xml.elements[0].elements)
            {
                if (element.name != "component")
                {
                    continue;
                }
                
                PackageComponentInfo packageComponentInfo = new PackageComponentInfo();
                packageComponentInfo.Id = element.GetAttribute("id");
                packageComponentInfo.Name = element.GetAttribute("name");
                packageComponentInfo.Path = "{0}{1}{2}".Fmt(packageDir, element.GetAttribute("path"), packageComponentInfo.Name);
                packageComponentInfo.Exported = element.GetAttribute("exported") == "true";
                
                packageInfo.PackageComponentInfos.Add(packageComponentInfo.Name, packageComponentInfo);

                ComponentInfo componentInfo = ParseComponent(packageInfo, packageComponentInfo);
                ComponentInfos.Add(componentInfo.Id, componentInfo);
            }

            return packageInfo;
        }

        private static ComponentInfo ParseComponent(PackageInfo packageInfo, PackageComponentInfo packageComponentInfo)
        {
            ComponentInfo componentInfo = new ComponentInfo();
            componentInfo.PackageId = packageInfo.Id;
            componentInfo.Id = packageComponentInfo.Id;
            componentInfo.Name = packageComponentInfo.Name;
            componentInfo.NameWithoutExtension = Path.GetFileNameWithoutExtension(packageComponentInfo.Name);
            componentInfo.Url = "ui://{0}{1}".Fmt(packageInfo.Id, packageComponentInfo.Id);
            componentInfo.Exported = packageComponentInfo.Exported;
            componentInfo.ComponentType = ComponentType.Component;

            XML xml = new XML(File.ReadAllText(packageComponentInfo.Path));
            foreach (XML element in xml.elements)
            {
                if (element.name == "displayList")
                {
                    componentInfo.DisplayList = element.elements;
                }
                else if (element.name == "controller")
                {
                    componentInfo.ControllerList.Add(element);
                }
                else if (element.name == "relation")
                {
                    
                }
                else
                {
                    ComponentType type = EnumHelper.FromString<ComponentType>(element.name);
                    if (type == ComponentType.None)
                    {
                        Debug.LogError("{0}类型没有处理！".Fmt(element.name));
                        continue;
                    } 
                    
                    if (type == ComponentType.ComboBox)
                    {
                        ExtralExportURLs.Add(element.GetAttribute("dropdown"));
                    }
                    
                    componentInfo.ComponentType = type;
                }
            }

            return componentInfo;
        }
        
        private static void GenCode()
        {
            foreach (ComponentInfo componentInfo in ComponentInfos.Values)
            {
                SpawnCodeComponent(componentInfo);
            }
            
            foreach (var kv in ExportedComponentInfos)
            {
                SpawnCodeBinder(PackageInfos[kv.Key], kv.Value);
            }
        }
        
        private static void SpawnCodeBinder(PackageInfo packageInfo, Dictionary<string, ComponentInfo> componentInfos)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("/** This is an automatically generated class by FUICodeSpawner. Please do not modify it. **/\n");
            
            sb.AppendLine("using FairyGUI;\n");
            sb.AppendFormat("namespace {0}\n", NameSpace);
            sb.AppendLine("{");
            sb.AppendFormat("\tpublic class {0}Binder\n", packageInfo.Name);
            sb.AppendLine("\t{");
            sb.AppendLine("\t\tpublic static void BindAll()");
            sb.AppendLine("\t\t{");

            foreach (ComponentInfo componentInfo in componentInfos.Values)
            {
                if (!componentInfo.Exported && !ExtralExportURLs.Contains(componentInfo.Url))
                {
                    continue;
                }
                
                sb.AppendFormat("\t\t\tUIObjectFactory.SetPackageItemExtension({0}{1}.URL, typeof({0}{1}));\n", ClassNamePrefix, componentInfo.NameWithoutExtension);
            }
            
            sb.AppendLine("\t\t}");
            sb.AppendLine("\t}");
            sb.AppendLine("}");
            
            string dir = "{0}{1}".Fmt(SpawnCodeDir, packageInfo.Name);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            
            string filePath = "{0}/{1}Binder.cs".Fmt(dir, packageInfo.Name);
            using FileStream fs = new FileStream(filePath, FileMode.Create);
            using StreamWriter sw = new StreamWriter(fs);
            sw.Write(sb.ToString());
            
            Debug.Log(sb.ToString());
        }

        private static readonly List<string> TypeNames = new List<string>();
        private static readonly List<string> VariableNames = new List<string>();
        private static readonly List<string> ControllerNames = new List<string>();
        private static readonly Dictionary<string, List<string>> ControllerPageNames = new Dictionary<string, List<string>>();
        private static void SpawnCodeComponent(ComponentInfo componentInfo)
        {
            if (!componentInfo.Exported && !ExtralExportURLs.Contains(componentInfo.Url))
            {
                return;
            }

            GatherVariable(componentInfo);

            if (VariableNames.Count == 0)
            {
                return;
            }
                
            GatherController(componentInfo);

            ExportedComponentInfos.Add(componentInfo.PackageId, componentInfo.Id, componentInfo);
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("/** This is an automatically generated class by FUICodeSpawner. Please do not modify it. **/\n");
            sb.AppendLine("using FairyGUI;");
            sb.AppendLine("using FairyGUI.Utils;\n");
            sb.AppendFormat("namespace {0}\n", NameSpace);
            sb.AppendLine("{");
            sb.AppendFormat("\tpublic partial class {0}{1}: GComponent\n", ClassNamePrefix, componentInfo.NameWithoutExtension);
            sb.AppendLine("\t{");

            foreach (var kv in ControllerPageNames)
            {
                sb.AppendFormat("\t\tpublic enum {0}_Page\n", kv.Key);
                sb.AppendLine("\t\t{");
                foreach (string pageName in kv.Value)
                {
                    sb.AppendFormat("\t\t\tPage_{0},\n", pageName);
                }
                sb.AppendLine("\t\t}\n");
            }
            
            for (int i = 0; i < ControllerNames.Count; i++)
            {
                sb.AppendFormat("\t\tpublic Controller {0};\n", ControllerNames[i]);
            }
            
            for (int i = 0; i < TypeNames.Count; i++)
            {
                string typeName = TypeNames[i];
                string variableName = VariableNames[i];
                sb.AppendFormat("\t\tpublic {0} {1};\n", typeName, variableName);
            }

            sb.AppendFormat("\t\tpublic const string URL = \"{0}\";\n\n", componentInfo.Url);

            sb.AppendFormat("\t\tpublic static {0}{1} CreateInstance()\n", ClassNamePrefix, componentInfo.NameWithoutExtension);
            sb.AppendLine("\t\t{");
            sb.AppendFormat("\t\t\treturn ({0}{1})UIPackage.CreateObject(\"{2}\", \"{1}\");\n", ClassNamePrefix, componentInfo.NameWithoutExtension, PackageInfos[componentInfo.PackageId].Name);
            sb.AppendLine("\t\t}\n");
            
            sb.AppendLine("\t\tpublic override void ConstructFromXML(XML xml)");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\tbase.ConstructFromXML(xml);\n");

            for (int i = 0; i < ControllerNames.Count; i++)
            {
                sb.AppendFormat("\t\t\t{0} = GetControllerAt({1});\n", ControllerNames[i], i);
            }
            
            for (int i = 0; i < TypeNames.Count; i++)
            {
                sb.AppendFormat("\t\t\t{0} = ({1})GetChildAt({2});\n", VariableNames[i], TypeNames[i], i);
            }
            sb.AppendLine("\t\t}");
            sb.AppendLine("\t}");
            sb.AppendLine("}");

            string dir = "{0}{1}".Fmt(SpawnCodeDir, PackageInfos[componentInfo.PackageId].Name);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            
            string filePath = "{0}/{1}{2}.cs".Fmt(dir, ClassNamePrefix, componentInfo.NameWithoutExtension);
            using FileStream fs = new FileStream(filePath, FileMode.Create);
            using StreamWriter sw = new StreamWriter(fs);
            sw.Write(sb.ToString());
        }

        private static void GatherController(ComponentInfo componentInfo)
        {
            ControllerNames.Clear();
            ControllerPageNames.Clear();
            foreach (XML controllerXML in componentInfo.ControllerList)
            {
                string controllerName = controllerXML.GetAttribute("name");
                if (!CheckControllerName(controllerName, componentInfo.ComponentType))
                {
                    continue;
                }

                ControllerNames.Add(controllerName);

                List<string> pageNames = new List<string>();
                string[] pages = controllerXML.GetAttribute("pages").Split(',');
                for (int i = 0; i < pages.Length; i++)
                {
                    string page = pages[i];
                    if (i % 2 == 1)
                    {
                        if (!string.IsNullOrEmpty(page))
                        {
                            pageNames.Add(page);
                        }
                    }
                }

                if (pageNames.Count == pages.Length / 2)
                {
                    ControllerPageNames.Add(controllerName, pageNames);
                }
            }
        }

        private static void GatherVariable(ComponentInfo componentInfo)
        {
            TypeNames.Clear();
            VariableNames.Clear();

            foreach (XML displayXML in componentInfo.DisplayList)
            {
                string variableName = displayXML.GetAttribute("name");
                if (displayXML.GetAttribute("id").StartsWith(variableName))
                {
                    continue;
                }

                if (!CheckVariableName(variableName, componentInfo.ComponentType))
                {
                    continue;
                }

                string typeName = GetTypeNameByDisplayXML(displayXML);
                if (string.IsNullOrEmpty(typeName))
                {
                    continue;
                }

                VariableNames.Add(variableName);
                TypeNames.Add(typeName);
            }
        }

        private static bool CheckControllerName(string controllerName, ComponentType componentType)
        {
            if (componentType == ComponentType.Button || componentType == ComponentType.ComboBox)
            {
                return controllerName != "button";
            }

            return true;
        }
        
        private static bool CheckVariableName(string variableName, ComponentType componentType)
        {
            if (variableName == "icon" || variableName == "text")
            {
                return false;
            }

            switch (componentType)
            {
                case ComponentType.Component:
                    break;
                case ComponentType.Button:
                case ComponentType.ComboBox:
                case ComponentType.Label:
                    if (variableName == "title")
                    {
                        return false;
                    }
                    break;
                case ComponentType.ProgressBar:
                    if (variableName == "bar" || variableName == "bar_v" || variableName == "ani")
                    {
                        return false;
                    }
                    break;
                case ComponentType.ScrollBar:
                    if (variableName == "arrow1" || variableName == "arrow2" || variableName == "grip" || variableName == "bar")
                    {
                        return false;
                    }
                    break;
                case ComponentType.Slider:
                    if (variableName == "bar" || variableName == "bar_v" || variableName == "grip" || variableName == "ani")
                    {
                        return false;
                    }
                    break;
                default:
                    throw new Exception("没有处理这种类型: {0}".Fmt(componentType));
            }

            return true;
        }
        
        private static string GetTypeNameByDisplayXML(XML displayXML)
        {
            string typeName = string.Empty;

            if (displayXML.name == "component")
            {
                ComponentInfo displayComponentInfo = ComponentInfos[displayXML.GetAttribute("src")];
                if (displayComponentInfo == null)
                {
                    throw new Exception("没找到对应类型：{0}".Fmt(displayXML.GetAttribute("src")));
                }
                
                if (displayComponentInfo.ComponentType == ComponentType.Component)
                {
                    typeName = "{0}{1}".Fmt(ClassNamePrefix, displayComponentInfo.NameWithoutExtension);
                }
                else
                {
                    typeName = ComponentTypeToClassType[displayComponentInfo.ComponentType];
                }
            }
            else if (displayXML.name == "text")
            {
                ObjectType objectType = displayXML.GetAttribute("input") == "true" ? ObjectType.textinput : ObjectType.textfield;
                typeName = ObjectTypeToClassType[objectType];
            }
            else if (displayXML.name == "group")
            {
                if (displayXML.GetAttribute("advanced") != "true")
                {
                    return typeName;
                }

                ObjectType objectType = EnumHelper.FromString<ObjectType>(displayXML.name);
                typeName = ObjectTypeToClassType[objectType];
            }
            else
            {
                ObjectType objectType = EnumHelper.FromString<ObjectType>(displayXML.name);

                try
                {
                    typeName = ObjectTypeToClassType[objectType];
                }
                catch (Exception e)
                {
                    Debug.LogError($"{objectType}没找到！");
                    Debug.LogError(e);
                }
            }

            return typeName;
        }
    }
}











