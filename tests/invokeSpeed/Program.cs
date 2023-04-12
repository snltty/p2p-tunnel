using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Reflection;

namespace invokeSpeed
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<InvokeTest>();
            Console.ReadLine();
        }
    }


    /// <summary>
    /// 接口
    /// </summary>
    public interface IMyInterface
    {
        public int Method();
    }

    /// <summary>
    /// 类
    /// </summary>
    public class MyClass : IMyInterface
    {
        public int Method()
        {
            return 0;
        }
    }

    /// <summary>
    /// 委托
    /// </summary>
    /// <returns></returns>
    public delegate int MyDelegate();


    [MemoryDiagnoser]
    public class InvokeTest
    {

        IMyInterface myInterface = new MyClass();
        MyClass myClass = new MyClass();
        MyDelegate myDelegate;
        MethodInfo method;

        [GlobalSetup]
        public void Setup()
        {
            method = myClass.GetType().GetMethod("Method")!;
            myDelegate = (MyDelegate)Delegate.CreateDelegate(typeof(MyDelegate), myClass, method);
        }


        /// <summary>
        /// 直接调用
        /// </summary>
        [Benchmark]
        public void ByClass()
        {
            for (int i = 0; i < 1000; i++)
            {
                myClass.Method();
            }
        }

        /// <summary>
        /// 委托调用
        /// </summary>
        [Benchmark]
        public void ByDelegate()
        {
            for (int i = 0; i < 1000; i++)
            {
                myDelegate();
            }
        }

        /// <summary>
        /// 接口调用
        /// </summary>
        [Benchmark]
        public void ByInterface()
        {
            for (int i = 0; i < 1000; i++)
            {
                myInterface.Method();
            }
        }

        /// <summary>
        /// 反射调用
        /// </summary>
        [Benchmark]
        public void ByReflection()
        {
            for (int i = 0; i < 1000; i++)
            {
                method.Invoke(myClass,Array.Empty<object>());
            }
        }
    }
}