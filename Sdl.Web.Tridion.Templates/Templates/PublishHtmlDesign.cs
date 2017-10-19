using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Sdl.Web.Tridion.Common;
using Tridion.ContentManager.CommunicationManagement;
using Tridion.ContentManager.ContentManagement;
using Tridion.ContentManager.ContentManagement.Fields;
using Tridion.ContentManager.Publishing.Rendering;
using Tridion.ContentManager.Templating;
using Tridion.ContentManager.Templating.Assembly;

namespace Sdl.Web.Tridion.Templates
{
    /// <summary>
    /// Publish HTML design by unpacking the templates and less variables and running grunt to build it.
    /// </summary>
    [TcmTemplateTitle("Publish HTML Design")]
    [TcmTemplateParameterSchema("resource:Sdl.Web.Tridion.Resources.PublishHtmlDesignParameters.xsd")]
    public class PublishHtmlDesign : TemplateBase
    {
        private const string SystemSgName = "_System";
        private const string HtmlDesignConfigNamespace = "http://www.sdl.com/web/schemas/html-design";
        private const string HtmlDesignConfigRootElementName = "HtmlDesignConfig";

        // Set of files to merge across modules
        private readonly Dictionary<string, List<string>> _mergeFileLines = new Dictionary<string, List<string>>
        {
            { @"src\system\assets\less\_custom.less", new List<string>()},
            { @"src\system\assets\less\_modules.less", new List<string>()},
            { @"src\templates\partials\module-scripts-header.hbs", new List<string>()},
            { @"src\templates\partials\module-scripts-footer.hbs", new List<string>()},
            { @"src\templates\partials\module-scripts-xpm.hbs", new List<string>()}
        };

        public override void Transform(Engine engine, Package package)
        {
            Initialize(engine, package);

            bool cleanup;
            // cleanup should be true by default (if not filled in)
            if (!package.TryGetParameter("cleanup", out cleanup, Logger))
			{
				cleanup = true;
            }

            string drive;
            package.TryGetParameter("drive", out drive, Logger);

            List<Binary> binaries = new List<Binary>();

            // Read values from HTML Design Configuration Component (which should be the Component used for this Component Presentation)
            Component inputComponent = GetComponent();
            if (inputComponent.Schema.NamespaceUri != HtmlDesignConfigNamespace || inputComponent.Schema.RootElementName != HtmlDesignConfigRootElementName)
            {
                throw new DxaException(
                    string.Format("Unexpected input Component {0} ('{1}'). Expecting HTML Design Configuration Component.", inputComponent.Id, inputComponent.Title)
                    );
            }
            ItemFields htmlDesignConfigFields = new ItemFields(inputComponent.Content, inputComponent.Schema);
            Component favIconComponent = htmlDesignConfigFields.GetMultimediaLink("favicon");
            string htmlDesignVersion = htmlDesignConfigFields.GetTextValue("version");

            // Publish version.json file
            IDictionary<string, string> versionData = new Dictionary<string, string> { { "version", htmlDesignVersion } };
            Binary versionJsonBinary = AddJsonBinary(versionData, inputComponent, Publication.RootStructureGroup, "version", variantId: "version");
            binaries.Add(versionJsonBinary);

            string tempFolder = GetTempFolder(drive);
            Directory.CreateDirectory(tempFolder);
            Logger.Debug("Created temp folder: " + tempFolder);

            try
            {
                // Unzip and merge files
                ProcessModules(tempFolder);
                
                string distFolder = BuildHtmlDesign(tempFolder);

                // Save favicon to disk (if available)
                if (favIconComponent != null)
                {
                    string favIconFilePath = Path.Combine(distFolder, "favicon.ico");
                    File.WriteAllBytes(favIconFilePath, favIconComponent.BinaryContent.GetByteArray());
                    Logger.Debug("Saved " + favIconFilePath);
                }

                // Publish all files from dist folder
                Publication pub = (Publication) inputComponent.ContextRepository;
                string rootStructureGroupWebDavUrl = pub.RootStructureGroup.WebDavUrl;
                RenderedItem renderedItem = engine.PublishingContext.RenderedItem;

                string[] distFiles = Directory.GetFiles(distFolder, "*.*", SearchOption.AllDirectories);
                foreach (string file in distFiles)
                {
                    Logger.Debug("Found " + file);

                    // Map the file path to a Structure Group
                    string relativeFolderPath = file.Substring(distFolder.Length, file.LastIndexOf('\\') - distFolder.Length);
                    Logger.Debug(string.Format("Relative folder path: '{0}'",relativeFolderPath));
                    string sgWebDavUrl = rootStructureGroupWebDavUrl + relativeFolderPath.Replace("system", SystemSgName).Replace('\\', '/');
                    StructureGroup structureGroup = engine.GetObject(sgWebDavUrl) as StructureGroup;
                    if (structureGroup == null)
                    {
                        throw new DxaException(string.Format("Cannot publish '{0}' because Structure Group '{1}' does not exist.", file, sgWebDavUrl));
                    }

                    // Add binary to package and publish
                    using (FileStream fs = File.OpenRead(file))
                    {
                        string filename = Path.GetFileName(file);
                        string extension = Path.GetExtension(file);
                        string variantId =  string.Format("dist-{0}-{1}", structureGroup.Id.ItemId, filename);
                        Item binaryItem = Package.CreateStreamItem(GetContentType(extension), fs);
                        Binary binary = renderedItem.AddBinary(
                            binaryItem.GetAsStream(),
                            filename,
                            structureGroup,
                            variantId,
                            inputComponent,
                            GetMimeType(extension)
                            );
                        binaryItem.Properties[Item.ItemPropertyPublishedPath] = binary.Url;
                        package.PushItem(filename, binaryItem);

                        binaries.Add(binary);
                        Logger.Info(string.Format("Added Binary '{0}' related to Component '{1}' ({2}) with variant ID '{3}'", 
                            binary.Url, inputComponent.Title, inputComponent.Id, variantId));
                    }
                }
            }
            finally
            {
                if (cleanup)
                {
                    Directory.Delete(tempFolder, true);
                    Logger.Debug("Removed temp folder " + tempFolder);
                }
                else
                {
                    Logger.Debug("Did not cleanup temp folder " + tempFolder);
                }
            }

            OutputSummary("Publish HTML Design", binaries.Select(b => b.Url));
        }

        private string GetTempFolder(string driveParameter)
        {
            int timestamp = Convert.ToInt32(DateTime.Now.ToString("HHmmssfff"));
            if (!string.IsNullOrEmpty(driveParameter) && char.IsLetter(driveParameter.First()))
            {
                // Supporting drive parameter for backward compatibility
                return driveParameter.First() + @":\_" + timestamp.ToString("x");
            }

            return Path.Combine(Path.GetTempPath(), timestamp.ToString("x"));
        }

        private string BuildHtmlDesign(string tempFolder)
        {
            string user = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c npm start --color=false",
                WorkingDirectory = tempFolder,
                CreateNoWindow = true,
                ErrorDialog = false,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardErrorEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8
            };
            using (Process cmd = new Process { StartInfo = info })
            {
                cmd.Start();
                using (StreamReader reader = cmd.StandardOutput)
                {
                    string output = reader.ReadToEnd();
                    if (!String.IsNullOrEmpty(output))
                    {
                        Logger.Info(output);
                    }
                }
                using (StreamReader reader = cmd.StandardError)
                {
                    string error = reader.ReadToEnd();
                    if (!string.IsNullOrEmpty(error))
                    {
                        Exception ex = new DxaException(error);
                        ex.Data.Add("Filename", info.FileName);
                        ex.Data.Add("Arguments", info.Arguments);
                        ex.Data.Add("User", user);
                        throw ex;
                    }
                }
                cmd.WaitForExit();
            }

            string distFolder = Path.Combine(tempFolder, "dist");
            if (!Directory.Exists(distFolder))
            {
                throw new DxaException("HTML Design build failed; dist folder is missing.");
            }
            return distFolder;
        }

        protected void ProcessModules(string tempFolder)
        {
            Dictionary<string, Component> modules = GetActiveModules();
            // TODO TSI-1081: get rid of hard-coded "core" name here
            foreach (KeyValuePair<string, Component> module in modules)
            {
                string moduleName = module.Key;
                if (moduleName != "core")
                {
                    ProcessModule(moduleName, module.Value, tempFolder);
                }
            }
            ProcessModule("core", modules["core"], tempFolder);

            // overwrite all merged files
            foreach (KeyValuePair<string, List<string>> mergeFile in _mergeFileLines)
            {
                string file = Path.Combine(tempFolder, mergeFile.Key);
                File.WriteAllText(file, String.Join(Environment.NewLine, mergeFile.Value));
                Logger.Debug("Saved " + file);
            }   
        }

        protected void ProcessModule(string moduleName, Component moduleComponent, string tempFolder)
        {
            Component designConfig = GetDesignConfigComponent(moduleComponent);
            if (designConfig != null)
            {
                ItemFields fields = new ItemFields(designConfig.Content, designConfig.Schema);
                Component zip = fields.GetComponentValue("design");
                Component variables = fields.GetComponentValue("variables");
                string code = fields.GetTextValue("code");
                string customLess = GetModuleCustomLess(variables, code);
                if (zip != null)
                {
                    // write binary contents as zipfile to disk
                    string zipfile = Path.Combine(tempFolder, moduleName + "-html-design.zip");
                    File.WriteAllBytes(zipfile, zip.BinaryContent.GetByteArray());

                    // unzip
                    using (ZipArchive archive = ZipFile.OpenRead(zipfile))
                    {
                        archive.ExtractToDirectory(tempFolder, true);
                    }

                    // add content from merge files if available
                    List<string> files = _mergeFileLines.Keys.Select(s => s).ToList();
                    foreach (string mergeFile in files)
                    {
                        string path = Path.Combine(tempFolder, mergeFile);
                        if (File.Exists(path))
                        {
                            foreach (string line in File.ReadAllLines(path))
                            {
                                if (!_mergeFileLines[mergeFile].Contains(line.Trim()))
                                {
                                    _mergeFileLines[mergeFile].Add(line.Trim());
                                }
                            }
                        }
                    }

                    // add custom less code block
                    if (!String.IsNullOrEmpty(customLess.Trim()))
                    {
                        _mergeFileLines["src\\system\\assets\\less\\_custom.less"].Add(customLess);
                    }
                }

                if (moduleName.Equals("core"))
                {
                    // unzip build files (nodejs, npm and grunt etc.)
                    Component buildFiles = fields.GetComponentValue("build");
                    if (buildFiles != null)
                    {
                        ProcessBuildFiles(buildFiles, tempFolder);
                    }
                }
            }
        }

        protected void ProcessBuildFiles(Component zip, string tempFolder)
        {
            // write binary contents as zipfile to disk
            string zipfile = Path.Combine(tempFolder, "build-files.zip");
            File.WriteAllBytes(zipfile, zip.BinaryContent.GetByteArray());

            // unzip
            using (ZipArchive archive = ZipFile.OpenRead(zipfile))
            {
                archive.ExtractToDirectory(tempFolder, true);
            }
        }

        private static Component GetDesignConfigComponent(Component moduleComponent)
        {
            ItemFields fields = new ItemFields(moduleComponent.Content, moduleComponent.Schema);
            return fields.GetComponentValue("designConfiguration");
        }

        private static string GetModuleCustomLess(Component variables, string code)
        {
            const string line = "@{0}: {1};";
            StringBuilder content = new StringBuilder();

            // save less variables to disk (if available) in unpacked zip structure
            if (variables != null)
            {
                // assuming all fields are text fields with a single value
                ItemFields itemFields = new ItemFields(variables.Content, variables.Schema);
                foreach (ItemField itemField in itemFields)
                {
                    string value = ((TextField)itemField).Value;
                    if (!String.IsNullOrEmpty(value))
                    {
                        content.AppendFormat(line, itemField.Name, ((TextField)itemField).Value);
                    }
                }
            }
            if (code != null)
            {
                content.Append(code);
            }
            return content.ToString();
        }

        private static ContentType GetContentType(string extension)
        {
            // remove dot if extension starts with it
            if (extension.StartsWith("."))
            {
                extension = extension.Substring(1);
            }

            switch (extension)
            {
                case "css":
                case "js":
                case "htc":
                    return ContentType.Text;
                case "gif":
                    return ContentType.Gif;
                case "jpg":
                case "jpeg":
                case "jpe":
                    return ContentType.Jpeg;
                case "ico":
                case "png":
                    return ContentType.Png;
                default:
                    return ContentType.Unknown;
            }
        }

        private static string GetMimeType(string extension)
        {
            // remove dot if extension starts with it
            if (extension.StartsWith("."))
            {
                extension = extension.Substring(1);
            }

            switch (extension)
            {
                case "css":
                    return "text/css";
                case "js":
                    return "application/x-javascript";
                case "htc":
                    return "text/x-component";
                case "gif":
                    return "image/gif";
                case "jpg":
                case "jpeg":
                case "jpe":
                    return "image/jpeg";
                case "ico":
                    return "image/x-icon";
                case "png":
                    return "image/png";
                case "svg":
                    return "image/svg+xml";
                case "eot":
                    return "application/vnd.ms-fontobject";
                case "woff":
                    return "application/x-woff";
                case "otf":
                    return "application/x-font-opentype";
                case "ttf":
                    return "application/x-font-ttf";
                default:
                    return "application/octet-stream";
            }
        }
    }
}
