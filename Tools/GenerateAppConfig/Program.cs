using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Mono.Cecil;

namespace GenerateAppConfig
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var outputFilePath = args[0];

            XNamespace assemblyBindingNamespace = "urn:schemas-microsoft-com:asm.v1";

            var dependentAssemblies = new List<XElement>(args.Length - 1);
            for (var i = 1; i < args.Length; i++)
            {
                var path = args[i];
                if (!string.Equals(Path.GetExtension(path), ".dll", StringComparison.OrdinalIgnoreCase))
                    continue;

                AssemblyDefinition asm;
                try
                {
                    asm = AssemblyDefinition.ReadAssembly(path);
                }
                catch
                {
                    continue;
                }

                using (asm)
                {
                    var name = asm.Name;

                    if (!name.HasPublicKey) continue;

                    var culture = name.Culture;
                    if (string.IsNullOrEmpty(culture)) culture = "neutral";

                    var version = name.Version.ToString();

                    dependentAssemblies.Add(new XElement(
                        assemblyBindingNamespace + "dependentAssembly",
                        new XElement(
                            assemblyBindingNamespace + "assemblyIdentity",
                            new XAttribute("name", name.Name),
                            new XAttribute("publicKeyToken", string.Concat(name.PublicKeyToken.Select(x => x.ToString("x2")))),
                            new XAttribute("culture", culture)
                        ),
                        new XElement(
                            assemblyBindingNamespace + "bindingRedirect",
                            new XAttribute("oldVersion", "0.0.0.0-" + version),
                            new XAttribute("newVersion", version)
                        )
                    ));
                }
            }

            var document = new XDocument(
                new XElement(
                    "configuration",
                    new XElement(
                        "startup",
                        new XElement(
                            "supportedRuntime",
                            new XAttribute("version", "v4.0"),
                            new XAttribute("sku", ".NETFramework,Version=v4.6")
                        )
                    ),
                    new XElement(
                        "runtime",
                        new XElement(
                            assemblyBindingNamespace + "assemblyBinding",
                            dependentAssemblies
                        )
                    )
                )
            );

            var outputDirectory = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrEmpty(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            using (var writer = new StreamWriter(File.OpenWrite(outputFilePath), new UTF8Encoding(false)))
                document.Save(writer);
        }
    }
}