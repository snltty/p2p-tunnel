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
            RectangleTest rectangleTest = new RectangleTest();
            //BenchmarkRunner.Run<Test>();
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
            new Rectangle(50,200,100,300),
            new Rectangle(300,200,100,200),

            new Rectangle(400,400,100,100),
        };

        public void UnionRectangles(Rectangle[] rects)
        {
            for (int i = 0; i < rects.Length; i++)
            {
                for (int j = i; j < rects.Length; j++)
                {
                    if (rects[i].X > rects[j].X)
                    {
                        Rectangle temp = rects[i];
                        rects[i] = rects[j];
                        rects[j] = temp;
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

        public static Rectangle Union(Rectangle a, Rectangle b)
        {
            int x1 = Math.Min(a.X, b.X);
            int x2 = Math.Max(a.X + a.Width, b.X + b.Width);
            int y1 = Math.Min(a.Y, b.Y);
            int y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);

            return new Rectangle(x1, y1, x2 - x1, y2 - y1);
        }
    }
}