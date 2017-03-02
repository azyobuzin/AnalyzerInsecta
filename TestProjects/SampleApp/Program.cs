using System;

namespace SampleApp
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"This class name is {nameof(Program)}.");
            Console.WriteLine($"The namespace name is {nameof(SampleApp)}.");
        }
    }
}
