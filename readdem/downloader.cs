using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Collections;
using System.Threading;
using System.Drawing;
using  OSGeo.GDAL;

namespace createterrain
{
    public class downloader
    {
        static ArrayList AllMission;
        static bool addmissioncomplete = false;
        public static void download(string[] args)
        {
            AllMission = new ArrayList();
            int minlayer = 8;
            int maxtlayer = 10;
            int threadcount = 100;
            while (threadcount-- > 0)
            {

                Thread thread = new Thread(new ThreadStart(downloadthread));
                thread.Name = threadcount.ToString();
                thread.Start();
            }
            for (int layer = minlayer; layer < maxtlayer; layer++)
            {
                int maxx = (int)Math.Pow(2, layer) * 2;
                int maxy = (int)Math.Pow(2, layer);
                for (int x = 0; x < maxx; x++)
                    for (int y = 0; y < maxy; y++)
                    {
                        while (AllMission.Count >= 10000)
                            System.Threading.Thread.Sleep(100);
                        System.Threading.Monitor.Enter(AllMission);
                        AllMission.Add(new int[] { layer, x, y, 0 });
                        System.Threading.Monitor.Exit(AllMission);

                    }
            }
            addmissioncomplete = true;
        }

        private static void downloadthread()
        {
            while (true)
            {
                try
                {
                    while (AllMission.Count == 0)
                    {
                        if (addmissioncomplete)
                            return;
                        System.Threading.Thread.Sleep(100);
                    }

                    System.Threading.Monitor.Enter(AllMission);
                    int[] values = (int[])AllMission[0];
                    AllMission.RemoveAt(0);
                    System.Threading.Monitor.Exit(AllMission);
                    int layer = values[0];
                    int x = values[1];
                    int y = values[2];
                    int trytime = values[3];
                    try
                    {
                        string url = "http://cesiumjs.org/smallterrain/" + layer.ToString() + "/" + x.ToString() + "/" + y.ToString() + ".terrain";
                        string locapath = @"D:\文武\3D\terraindem\" + layer.ToString() + "/" + x.ToString() + "/" + y.ToString() + ".terrain";
                        if (System.IO.File.Exists(locapath))
                            continue;
                        string path = System.IO.Path.GetDirectoryName(locapath);
                        if (!System.IO.Directory.Exists(path))
                            System.IO.Directory.CreateDirectory(path);
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                        request.Accept = @"application/vnd.quantized-mesh,application/octet-stream;q=0.9,*/*;q=0.01";
                        request.UserAgent = @"Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/38.0.2125.111 Safari/537.36";
                        request.Headers.Add("Accept-Encoding", "gzip,deflate,sdch");
                        request.Timeout = 2000;
                        WebResponse response = request.GetResponse();
                        SaveBinaryFile(response, locapath);
                        Console.WriteLine(locapath);
                    }
                    catch (Exception)
                    {
                        trytime++;
                        values[3] = trytime;
                        if (trytime < 3)
                        {
                            System.Threading.Monitor.Enter(AllMission);
                            AllMission.Add(values);
                            System.Threading.Monitor.Exit(AllMission);
                        }
                        else
                            Console.WriteLine("下载失败" + layer.ToString() + "/" + x.ToString() + "/" + y.ToString());
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }

        }

        static int SaveBinaryFile(WebResponse response, string FileName)
        {
            int filesize = 0;
            byte[] buffer = new byte[1024];
            if (File.Exists(FileName))
                File.Delete(FileName);
            MemoryStream outStream = new MemoryStream();
            Stream inStream = response.GetResponseStream();
            int len;
            do
            {
                len = inStream.Read(buffer, 0, buffer.Length);
                filesize += len;
                if (len > 0)
                    outStream.Write(buffer, 0, len);
            }
            while (len > 0);

            inStream.Close();
            response.Close();

            byte[] data = outStream.GetBuffer();
            Array.Resize(ref data, filesize);
            MemoryStream stream = new MemoryStream(data);

            System.IO.Compression.GZipStream compressionStream = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress);
            BinaryReader reader = new BinaryReader(compressionStream);


            MemoryStream deoutStream = new MemoryStream();
            filesize = 0;
            while (true)
            {
                try
                {
                    byte da = reader.ReadByte();
                    deoutStream.WriteByte(da);
                    filesize++;
                }
                catch (EndOfStreamException)
                {
                    break;
                }
            }

            reader.Close();
            byte[] doutdata = deoutStream.GetBuffer();
            Array.Resize(ref doutdata, filesize);
            File.WriteAllBytes(FileName, doutdata);
            outStream.Close();
            return filesize;
        }
    }
}
