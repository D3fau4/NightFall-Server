using LibHac;
using LibHac.FsSystem;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace NightFall_Server
{
    internal class Server
    {
        public static void generateJson(string firmware, string pathkey, string firmver, string fwint, string output)
        {
            Keyset keyset = ExternalKeyReader.ReadKeyFile(pathkey);
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            int files = Directory.GetFiles(firmware).Length;
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();
                writer.WritePropertyName("fw_info");
                writer.WriteStartObject();
                writer.WritePropertyName("version");
                writer.WriteValue(firmver);
                writer.WritePropertyName("IsExfat");
                writer.WriteValue(true);
                writer.WritePropertyName("files");
                writer.WriteValue(files);
                writer.WriteEnd();
                writer.WritePropertyName("titleids");
                writer.WriteStartArray();
                // escribir todos los titleid
                ListTitleid(firmware, keyset, writer);
                writer.WriteEndArray();
                writer.WritePropertyName("programid");
                writer.WriteStartObject();
                Listnca(firmware, fwint, keyset, writer);
                writer.WriteEndObject();
                writer.WriteEnd();
            }
            File.WriteAllText(Path.Combine(output, fwint), sb.ToString());
        }
        public static void generateServerFS(string output)
        {
            Directory.CreateDirectory(Path.Combine(output, "c", "c"));
            Directory.CreateDirectory(Path.Combine(output, "c", "a"));
        }

        public static void CopyNCAfiles(string ncafolder, string output)
        {
            DirectoryInfo folderInfo = new DirectoryInfo(ncafolder);
            FileInfo[] files = folderInfo.GetFiles("*.nca", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                Console.WriteLine(files[i].Name);
                if (files[i].Name.Contains(".cnmt.nca"))
                {
                    File.Copy(Path.Combine(ncafolder, files[i].Name), Path.Combine(output, "c", "a", files[i].Name.Replace(".cnmt.nca", "")), true);
                }
                else
                {
                    File.Copy(Path.Combine(ncafolder, files[i].Name), Path.Combine(output, "c", "c", files[i].Name.Replace(".nca", "")), true);
                }
            }
        }

        public static void generateLastInfo(string intfw, string firmwarever, string output)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;

                writer.WriteStartObject();
                writer.WritePropertyName("Firmwver");
                writer.WriteValue(firmwarever);
                writer.WritePropertyName("intfw");
                writer.WriteValue(intfw);
            }
            File.WriteAllText(Path.Combine(output, "info"), sb.ToString());
        }

        public static void ListTitleid(string sdfs, Keyset keyset, JsonWriter writer)
        {
            SwitchFs switchFs;
            LocalFileSystem baseFs = new LocalFileSystem(sdfs);

            switchFs = SwitchFs.OpenNcaDirectory(keyset, baseFs);
            string lasttitleid = "";
            foreach (SwitchFsNca nca in switchFs.Ncas.Values.OrderBy(x => x.Nca.Header.TitleId))
            {
                if (nca.Nca.Header.TitleId.ToString("X16") == lasttitleid)
                {
                    continue;
                }
                lasttitleid = nca.Nca.Header.TitleId.ToString("X16");
                writer.WriteValue(nca.Nca.Header.TitleId.ToString("X16"));
            }

        }
        public static void Listnca(string sdfs, string fwint, Keyset keyset, JsonWriter writer)
        {
            SwitchFs switchFs;
            LocalFileSystem baseFs = new LocalFileSystem(sdfs);
            switchFs = SwitchFs.OpenNcaDirectory(keyset, baseFs);
            string lasttitleid = "";
            string lastncaid = "";
            string type;
            foreach (SwitchFsNca nca in switchFs.Ncas.Values.OrderBy(x => x.Nca.Header.TitleId))
            {

                if (nca.Nca.Header.TitleId.ToString("X16") != lasttitleid)
                {
                    writer.WritePropertyName(nca.Nca.Header.TitleId.ToString("X16"));
                    writer.WriteStartObject();
                    writer.WritePropertyName(nca.Nca.Header.ContentType.ToString());
                    writer.WriteValue(nca.NcaId);
                    // solo contiene META
                    if (int.Parse(fwint) >= 738197944)
                    {
                        if ("0100000000000825" == nca.Nca.Header.TitleId.ToString("X16") || "0100000000000029" == nca.Nca.Header.TitleId.ToString("X16") || "0100000000000816" == nca.Nca.Header.TitleId.ToString("X16"))
                        {
                            writer.WriteEnd();
                        }
                    }
                    else if ("010000000000001B" == nca.Nca.Header.TitleId.ToString("X16") || "0100000000000029" == nca.Nca.Header.TitleId.ToString("X16") || "0100000000000816" == nca.Nca.Header.TitleId.ToString("X16"))
                    {
                        writer.WriteEnd();
                    }

                    lastncaid = nca.NcaId;
                    lasttitleid = nca.Nca.Header.TitleId.ToString("X16");
                }
                if (lastncaid != nca.NcaId && lasttitleid == nca.Nca.Header.TitleId.ToString())
                {
                    if (nca.Nca.Header.ContentType.ToString() == "Data" || nca.Nca.Header.ContentType.ToString() == "PublicData")
                    {
                        type = "Program";
                    }
                    else
                    {
                        type = nca.Nca.Header.ContentType.ToString();
                    }
                    writer.WritePropertyName(type);
                    writer.WriteValue(nca.NcaId);
                    writer.WriteEnd();
                    lastncaid = nca.NcaId;
                    lasttitleid = nca.Nca.Header.TitleId.ToString("X16");
                }
                else if (lastncaid != nca.NcaId && lasttitleid != nca.Nca.Header.TitleId.ToString())
                {
                    writer.WritePropertyName(nca.Nca.Header.ContentType.ToString());
                    writer.WriteValue(nca.NcaId);
                    writer.WriteEnd();
                    lastncaid = nca.NcaId;
                    lasttitleid = nca.Nca.Header.TitleId.ToString("X16");

                }
            }
        }
    }
}