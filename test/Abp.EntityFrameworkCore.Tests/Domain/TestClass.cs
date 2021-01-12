namespace Abp.EntityFrameworkCore.Tests.Domain
{
    public class TestClass
    {
        public string Name { get; set; }
    }

    public class SubClass : TestClass
    {
        public int Age { get; set; }
    }
}