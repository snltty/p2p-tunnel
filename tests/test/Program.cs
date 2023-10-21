using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using common.libs.extends;
using System.Buffers.Binary;
using System.Net;
using System.Text;

namespace test
{
    internal class Program
    {
        static unsafe void Main(string[] args)
        {
            /*
            RectangleTest rectangleTest = new RectangleTest();
            rectangleTest.UnionRectangles(rectangleTest.rects);
            for (int i = 0; i < rectangleTest.rects.Length; i++)
            {
                if (rectangleTest.rects[i].Remove == false)
                {
                    Console.WriteLine($"{rectangleTest.rects[i].X},{rectangleTest.rects[i].Y},{rectangleTest.rects[i].Width},{rectangleTest.rects[i].Height}");
                }
            }
            */
            BenchmarkRunner.Run<Test>();
        }
    }

    [MemoryDiagnoser]
    public unsafe partial class Test
    {
        [GlobalSetup]
        public void Startup()
        {
        }

        RectangleTest rectangleTest = new RectangleTest();

        [Benchmark]
        public void Test1()
        {
            rectangleTest.UnionRectangles(rectangleTest.rects);
        }


    }

    public class RectangleTest
    {
       public Rectangle[] rects = new Rectangle[] {
             new Rectangle(0,100,100,100),
            new Rectangle(50,50,100,300),
            new Rectangle(300,200,100,200),

            new Rectangle(380,300,100,100),
        };

        public void UnionRectangles(Rectangle[] rects)
        {
            for (int i = 0; i < rects.Length; i++)
            {
                if (rects[i].Remove) continue;
                for (int j = i+1; j < rects.Length; j++)
                {
                    if (rects[j].Remove) continue;
                    if (rects[i].IntersectsWith( rects[j]))
                    {
                        rects[i] = rects[i].Union(rects[j]);
                        rects[j].Remove = true;
                    }
                }
            }
        }
    }


    public partial struct Rectangle
    {
        public Rectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public bool Remove { get; set; }

        public readonly bool IntersectsWith(Rectangle rect) =>
            (rect.X < X + Width) && (X < rect.X + rect.Width) &&
            (rect.Y < Y + Height) && (Y < rect.Y + rect.Height);

        public readonly Rectangle Union(Rectangle b)
        {
            Rectangle a = this;
            int x1 = Math.Min(a.X, b.X);
            int x2 = Math.Max(a.X + a.Width, b.X + b.Width);
            int y1 = Math.Min(a.Y, b.Y);
            int y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);

            return new Rectangle(x1, y1, x2 - x1, y2 - y1);
        }

        public static Rectangle[] UnionRectangles(Rectangle[] rects)
        {
            for (int i = 0; i < rects.Length; i++)
            {
                if (rects[i].Remove) continue;
                for (int j = i + 1; j < rects.Length; j++)
                {
                    if (rects[j].Remove) continue;
                    if (rects[i].IntersectsWith(rects[j]))
                    {
                        rects[i] = rects[i].Union(rects[j]);
                        rects[j].Remove = true;
                    }
                }
            }
            return rects;
        }
    }
}