using NUnit.Framework;
using reillyd_service;

namespace reillyd_tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void CanCallServiceProject()
        {
            Assert.AreEqual(Worker.Tester(), true);
        }
    }
}