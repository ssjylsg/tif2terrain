using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Collections;
using System.Threading;
using OSGeo.GDAL;

namespace readdem
{
    class Program
    {

        /// <summary>
        /// 计算一个terrain文件的坐标左下角
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>

        static double[] CountLb(int x, int y, int z)
        {
            double size = 180 / Math.Pow(2, z);
            double L = -180 + x * size;
            double B = -90 + y * size;
            return new double[] { L, B };
        }
        /// <summary>
        /// 计算一个坐标在指定层所在的行列号
        /// </summary>
        /// <param name="L"></param>
        /// <param name="B"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        static int[] CountLocation(double L, double B, int layer)
        {
            double size = 180 / Math.Pow(2, layer);
            int x = Convert.ToInt32((L + 180) / size);
            int y = Convert.ToInt32((B + 90) / size);

            double[] coors = CountLb(x, y, layer);
            return new int[] { x, y };
        }
        /// <summary>
        /// 写一个指定的terrain文件
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        static void WriteterrainFile(int x, int y, int z)
        {
            string filepath = basedir + z.ToString() + "/" + x.ToString() + "/" + y.ToString() + ".terrain";
            if (File.Exists(filepath))
                File.Delete(filepath);//return;
            string dir = System.IO.Path.GetDirectoryName(filepath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            FileStream stream = new FileStream(filepath, FileMode.Create);
            BinaryWriter writer = new BinaryWriter(stream);
            //这个单元的大小
            double size = 180 / Math.Pow(2, z);
            //左上角坐标
            double L = -180 + x * size;
            double B = -90 + y * size + size;
            //是65*65个,有一排或者一列与相邻的重复
            double smallsize = size / 64;
            for (int i = 0; i < 65; i++)
                for (int j = 0; j < 65; j++)
                {
                    double cl = L + j * smallsize;
                    double cb = B - i * smallsize;
                    double height = Getheight(cl, cb);
                    UInt16 value = Convert.ToUInt16((height + 1000) * 5);
                    writer.Write(value);
                }
            //最后写入15或者0
            if (z >= 12)
                writer.Write(Convert.ToInt16(0));
            else
                writer.Write(Convert.ToInt16(15));
            writer.Flush();
            writer.Close();
            stream.Close();
        }
        static Random rd = new Random();
        //需要覆盖这个方法，通过读取tiff取得真实的高度
        static double Getheight(double L, double B)
        {
            return demfiles.getheight(L, B);

        }
        /// <summary>
        /// 生成指定区域指定层的文件
        /// </summary>
        static void WriteFile(double minl, double maxl, double minb, double maxb, int minlayer, int maxtlayer)
        {
            int count = 0;

            for (int layer = minlayer; layer <= maxtlayer; layer++)
            {
                //左下角所在的行列号
                int[] lb = CountLocation(minl, minb, layer);
                int[] rt = CountLocation(maxl, maxb, layer);
                count += (rt[0] - lb[0] + 1) * (rt[1] - lb[1] + 1);
            }
            float location = 0;
            for (int layer = minlayer; layer <= maxtlayer; layer++)
            {
                //左下角所在的行列号
                int[] lb = CountLocation(minl, minb, layer);
                int[] rt = CountLocation(maxl, maxb, layer);
                for (int x = lb[0]; x <= rt[0]; x++)
                {
                    for (int y = lb[1]; y <= rt[1]; y++)
                    {
                        WriteterrainFile(x, y, layer);
                        float percent = location++ / count * 100;
                        Console.CursorLeft = 0;
                        Console.Write("                ");
                        Console.CursorLeft = 0;
                        Console.Write("{0}%", percent);
                    }
                }
            }
            Console.WriteLine();
        }

        static demfilemager demfiles;
        static string basedir = "d:/dem/";
        static void Main(string[] args)
        {
            //前7层建议自己到到CES上去下，下载地址   string url = "http://cesiumjs.org/smallterrain/" + layer.ToString() + "/" + x.ToString() + "/" + y.ToString() + ".terrain";

            Console.WriteLine("geotiff生成terrain文件工具 by www.earthg.cn 联系方式:earthgoing@163.com");
            OSGeo.GDAL.Gdal.AllRegister();
            Console.WriteLine("请输入GeoTiff文件夹存放的目录并回车:");
            //全部DEM放在这个地方
            string dir = Console.ReadLine();
            if (!Directory.Exists(dir))
            {
                Console.WriteLine("输入的文件夹不存在，程序退出!");
                Console.ReadKey();
                return;
            }
            demfiles = new demfilemager(dir);
            Console.WriteLine("请输入要输出的文件夹位置并回车:");
            basedir = Console.ReadLine();
            if (!basedir.EndsWith(Path.DirectorySeparatorChar.ToString()))
                basedir += Path.DirectorySeparatorChar;
            if (!Directory.Exists(basedir))
            {
                try
                {
                    Directory.CreateDirectory(basedir);
                }
                catch (Exception)
                {
                    Console.WriteLine("输出文件夹不合法，程序退出!");
                    Console.ReadKey();
                    return;
                }
            }
            while (true)
            {
                Console.WriteLine("请输入要输出的数据范围并回车(最小L,最大L,最小B,最大B,最小层，最大层)，输入END结束");
                string line = Console.ReadLine();
                if (line.ToUpper().IndexOf("END") >= 0)
                    break;
                string[] values = line.Split(new char[] { ',' });
                if (values.Length != 6)
                {
                    Console.WriteLine("参数错误,请重新输入");
                    continue;
                }
                Console.WriteLine("开始写入");
                //要生成的范围，最小L，最大L，最小B，最大B，最小层（7），最大层（12）,后面两个不要变
                WriteFile(double.Parse(values[0]), double.Parse(values[1]), double.Parse(values[2]), double.Parse(values[3]), int.Parse(values[4]), int.Parse(values[5]));
                Console.WriteLine("写入结束");
            }

            //string filepath = @"P:\3D\terraindem\10\1685\745.terrain";
            //var t = countLB(1685, 745, 10);
            //for (int z = 0; z < 8; z++)
            //{
            //    int xcount = (int)Math.Pow(2, z) * 2;
            //    int ycount = (int)Math.Pow(2, z);

            //}
            //return;

            //BinaryReader reader = new BinaryReader(new FileStream(filepath, FileMode.Open));
            //for (int i = 0; i < 65; i++)
            //{
            //    for (int j = 0; j < 65; j++)
            //    {
            //        int location = (i * 65 + j) * 2;
            //        byte[] data = reader.ReadBytes(2);
            //        int height = ((data[0] + data[1] * 256) / 5) - 1000;
            //        Console.Write(height.ToString().PadLeft(5) + " ");
            //    }
            //    Console.WriteLine();
            //}
            //Console.ReadLine();
            //        return;
        }

    }
}
