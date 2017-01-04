using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Drawing;

namespace readdem
{
    public class demfile
    {
        public double L;
        public double B;
        public double L2;
        public double B2;

        public double getheight(double L, double B)
        {
            double x = ((gt[5] * L - gt[2] * B) - (gt[0] * gt[5] - gt[3] * gt[2])) / (gt[1] * gt[5] - gt[4] * gt[2]);
            double y = ((gt[4] * L - gt[1] * B) - (gt[0] * gt[4] - gt[3] * gt[1])) / (gt[2] * gt[4] - gt[5] * gt[1]);

            int x1 = (int)(x + 0.5);
            int y1 = (int)(y + 0.5);
            if (x1 == this.XSize)
                XSize -= 1;
            if (y1 == this.YSize)
                YSize -= 1;
            double height = databuf[y1 * XSize + x1];
            return height;
        }
        //数据缓存
        private double[] databuf;
        //变换矩阵
        private double[] gt;
        private int XSize, YSize;
        string name;
        public demfile(string filename)
        {
            name = System.IO.Path.GetFileNameWithoutExtension(filename);
            var ds = OSGeo.GDAL.Gdal.Open(filename, OSGeo.GDAL.Access.GA_ReadOnly);
            XSize = ds.RasterXSize;
            YSize = ds.RasterYSize;
            // 获取栅格数据的长和宽
            int count = ds.RasterCount;  // 获取栅格数据的点的数量
            OSGeo.GDAL.Band demband = ds.GetRasterBand(1); // 获取第一个band         
            gt = new double[6];
            ds.GetGeoTransform(gt);             // 获取屏幕坐标转换到实际地理坐标的参数
            double nodatavalue;
            int hasval;

            demband.GetNoDataValue(out nodatavalue, out hasval);    // 获取没有数据的点的值
            databuf = new double[XSize * YSize];
            demband.ReadRaster(0, 0, ds.RasterXSize, ds.RasterYSize, databuf, ds.RasterXSize, ds.RasterYSize, 0, 0); // 读取数据到缓冲区中}
            System.Drawing.Drawing2D.Matrix matric = new System.Drawing.Drawing2D.Matrix((float)gt[1], (float)gt[2], (float)gt[4], (float)gt[5], (float)gt[0], (float)gt[3]);
            Point p1=new Point(0,YSize);
            Point p2=new Point(XSize,0);
            Point[] ps=new Point[]{p1,p2};
            matric.TransformPoints(ps);
            this.L = ps[0].X;
            this.B = ps[0].Y;
            this.L2 = ps[1].X;
            this.B2 = ps[1].Y;
            demband.Dispose();
            ds.Dispose();
            GC.GetTotalMemory(true);
        }
        public override string ToString()
        {
            return string.Format("文件名{0}: 经度范围:{1}-{2},纬度范围:{3}-{4}", name, L, L2, B, B2);
        }
    }
    public class demfilemager
    {
        ArrayList demfiles;
        public demfilemager(string dir)
        {
            demfiles = new ArrayList();
            string[] files = System.IO.Directory.GetFiles(dir, "*.tif");
            Console.WriteLine("发现有tif文件数:{0}", files.Length);
            foreach (string file in files)
            {
                try
                {
                    demfile item = new demfile(file);
                    demfiles.Add(item);
                    Console.WriteLine(item.ToString());
                }
                catch (OutOfMemoryException )
                {
                    Console.WriteLine("内存不足，剩下的文件将不再读取");
                    break;
                }
                catch (Exception er)
                {
                    Console.WriteLine("读取{0}时发生以下错误:{1}", file, er.Message);
                    Console.WriteLine(er.StackTrace);
                }
            }
        }
        public double getheight(double L, double B)
        {
            foreach (demfile item in demfiles)
            {
                if (item.L < L && item.L2>L&& item.B <= B&&item.B2>=B)
                    return item.getheight(L, B);
            }
            return 0;
        }
    }
}
