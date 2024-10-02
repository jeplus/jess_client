using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ensim.Module;
using System.Windows.Forms;

namespace ensim.TestProject
{
    [TestClass]
    public class ensim_UnitTest
    {       
        [TestMethod]
        public void TestMethod_CheckSuccessfulLogOn()
        {
            bool result = JESSProcess.LogOnTransaction("user", "test");            
            Assert.IsTrue(result==true,"Log on transaction successfull");            
           
        }

        [TestMethod]
        public void TestMethod_CheckFailedLogOn()
        {
            bool result = JESSProcess.LogOnTransaction("user", "testPassword");
            Assert.IsTrue(result == false, "Log on transaction failed");

        }

        [TestMethod]
        public void TestMethod_CheckSuccessfulCheckIn()
        {
            bool result = JESSProcess.CheckInTransaction();            
            Assert.IsTrue(result == true, "Check-In transaction successfull");
        }

        [TestMethod]
        public void TestMethod_CheckFailedCheckIn()
        {
            bool result = JESSProcess.CheckInTransaction();
            Assert.IsTrue(result == false, "Check-In transaction failed");

        }

    }
}
