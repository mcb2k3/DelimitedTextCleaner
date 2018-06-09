using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Sql2Go.DelimitedTextCleaner.Tests
{
    [TestClass()]
    public class CleanerTests
    {
        const string commaTextLine1 = "First,Second,Third,Fourth";
        const string commaTextLine1Enclosed = "\"First\",\"Second\",\"Third\",\"Fourth\"";
        const string commaTextLine1Pipe = "First|Second|Third|Fourth";
        const string commaTextLine1Tab = "First\tSecond\tThird\tFourth";

        const string commaTextLine2 = "123,abc,\",\",\"\"\"\"";
        const string commaTextLine2Enclosed = "\"123\",\"abc\",\",\",\"\"\"\"";
        const string commaTextLine2Pipe = "123|abc|\"|\"|\"\"\"\"";
        const string commaTextLine2Tab = "123\tabc\t\"\t\"\t\"\"\"\"";

        const string commaTextLine2FieldMissing = "123,abc,\",\"";
        const string commaTextLine2FieldMissingReturned = "123,abc,\",\",";

        const string commaTextLine2ExtraField = "123,abc,\",\",\"\"\"\",Extra";
        const string commaTextLine2ExtraFieldReturned = "123,abc,\",\",\"\"\",Extra\"";

        const string commaTextLine2StrayTextQuote = "1\"23,abc,\",\",\"\"\"\"";
        const string commaTextLine2StrayTextQuoteReturned = "\"1\"\"23\",abc,\",\",\"\"\"\"";

        const string commaTextLine3 = "1st,2nd,3rd,";

        const string commaTextLine4 = "1st,2nd,3rd,\"4thpt1\r\n4thpt2\"\r\n";

        readonly string[] textLine1Array =
            new string[] { "First", "Second", "Third", "Fourth" };
        readonly string[] textLine2Array =
            new string[] { "123", "abc", ",", "\"" };
        readonly string[] textLine3Array =
            new string[] { "1st", "2nd", "3rd", "" };
        readonly string[] textLine4Array =
            new string[] { "1st", "2nd", "3rd", "4thpt1\r\n4thpt2" };

        /// <summary>
        /// Invoke constructor and confirm empty results
        /// </summary>
        [TestMethod()]
        public void CleanerConstructorTest()
        {
            Cleaner cleaner;
            String[] retVal;

            cleaner = new Cleaner();
            Assert.IsNotNull(cleaner);
            retVal = cleaner.ReturnHeaders();
            Assert.AreEqual(0, retVal.Length);
            retVal = cleaner.ReturnFields();
            Assert.AreEqual(0, retVal.Length);

            cleaner = new Cleaner('|', true);
            Assert.IsNotNull(cleaner);
            retVal = cleaner.ReturnHeaders();
            Assert.AreEqual(0, retVal.Length);
            retVal = cleaner.ReturnFields();
            Assert.AreEqual(0, retVal.Length);

            cleaner = new Cleaner('|', false);
            Assert.IsNotNull(cleaner);
            retVal = cleaner.ReturnHeaders();
            Assert.AreEqual(0, retVal.Length);
            retVal = cleaner.ReturnFields();
            Assert.AreEqual(0, retVal.Length);
        }

        /// <summary>
        /// Test CleanText with default field delimiter (comma) and results not enclosed
        /// </summary>
        [TestMethod()]
        public void CleanTextTestDefaultFieldDelimiterNoEnclose()
        {
            Cleaner cleaner = new Cleaner();
            Assert.IsNotNull(cleaner);

            var result1 = cleaner.CleanText(commaTextLine1);        //Header
            Assert.IsTrue(result1);

            var resultText1 = cleaner.ReturnText();
            Assert.AreEqual<string>(commaTextLine1, resultText1);

            var result2 = cleaner.CleanText(commaTextLine2);        //Data
            Assert.IsTrue(result2);

            var resultText2 = cleaner.ReturnText();                 //More data
            Assert.AreEqual<string>(commaTextLine2, resultText2);
        }

        /// <summary>
        /// Test CleanText with default field delimiter (comma) and results enclosed
        /// </summary>
        [TestMethod()]
        public void CleanTextTestDefaultFieldDelimiterWithEnclose()
        {
            Cleaner cleaner = new Cleaner();
            Assert.IsNotNull(cleaner);

            var result1 = cleaner.CleanText(commaTextLine1);        //Header
            Assert.IsTrue(result1);

            var resultText1 = cleaner.ReturnText(true);
            Assert.AreEqual<string>(commaTextLine1Enclosed, resultText1);

            var result2 = cleaner.CleanText(commaTextLine2);        //Data
            Assert.IsTrue(result2);

            var resultText2 = cleaner.ReturnText(true);             //More data, enclose
            Assert.AreEqual<string>(commaTextLine2Enclosed, resultText2);
        }

        /// <summary>
        /// Test CleanText with pipe field delimiter (|) and results not enclosed
        /// </summary>
        [TestMethod()]
        public void CleanTextTestPipeFieldDelimiterNoEnclose()
        {
            Cleaner cleaner = new Cleaner('|', true);
            Assert.IsNotNull(cleaner);

            var result1 = cleaner.CleanText(commaTextLine1Pipe);        //Header
            Assert.IsTrue(result1);

            var resultText1 = cleaner.ReturnText();
            Assert.AreEqual<string>(commaTextLine1Pipe, resultText1);

            var result2 = cleaner.CleanText(commaTextLine2Pipe);        //Data
            Assert.IsTrue(result2);

            var resultText2 = cleaner.ReturnText();                 //More data
            Assert.AreEqual<string>(commaTextLine2Pipe, resultText2);
        }

        /// <summary>
        /// Test CleanText with pipe field delimiter (|) and results not enclosed
        /// </summary>
        [TestMethod()]
        public void CleanTextTestTabFieldDelimiterNoEnclose()
        {
            Cleaner cleaner = new Cleaner('\t', true);
            Assert.IsNotNull(cleaner);

            var result1 = cleaner.CleanText(commaTextLine1Tab);        //Header
            Assert.IsTrue(result1);

            var resultText1 = cleaner.ReturnText();
            Assert.AreEqual<string>(commaTextLine1Tab, resultText1);

            var result2 = cleaner.CleanText(commaTextLine2Tab);        //Data
            Assert.IsTrue(result2);

            var resultText2 = cleaner.ReturnText();                 //More data
            Assert.AreEqual<string>(commaTextLine2Tab, resultText2);
        }

        [TestMethod()]
        public void ReconcileFieldCountTestFieldMissing()
        {
            Cleaner cleaner = new Cleaner();
            Assert.IsNotNull(cleaner);

            var result1 = cleaner.CleanText(commaTextLine1);        //Header
            Assert.IsTrue(result1);

            var resultText1 = cleaner.ReturnText();
            Assert.AreEqual<string>(commaTextLine1, resultText1);

            var result2 = cleaner.CleanText(commaTextLine2FieldMissing);    //Data
            Assert.IsTrue(result2);

            int reconcileResult = cleaner.ReconcileFieldCount();
            Assert.AreEqual(-1, reconcileResult);

            var resultText2 = cleaner.ReturnText();                 //More data
            Assert.AreEqual<string>(commaTextLine2FieldMissingReturned, resultText2);
        }

        [TestMethod()]
        public void ReconcileFieldCountTestExtraField()
        {
            Cleaner cleaner = new Cleaner();
            Assert.IsNotNull(cleaner);

            var result1 = cleaner.CleanText(commaTextLine1);        //Header
            Assert.IsTrue(result1);

            var resultText1 = cleaner.ReturnText();
            Assert.AreEqual<string>(commaTextLine1, resultText1);

            var result2 = cleaner.CleanText(commaTextLine2ExtraField);    //Data
            Assert.IsTrue(result2);

            int reconcileResult = cleaner.ReconcileFieldCount();
            Assert.AreEqual(1, reconcileResult);

            var resultText2 = cleaner.ReturnText();                 //More data
            Assert.AreEqual<string>(commaTextLine2ExtraFieldReturned, resultText2);
        }

        /// <summary>
        /// Test CleanText with default field delimiter (comma) and results not enclosed
        /// </summary>
        [TestMethod()]
        public void CleanTextTestDefaultFieldDelimiterStrayTextQuote()
        {
            Cleaner cleaner = new Cleaner();
            Assert.IsNotNull(cleaner);

            var result1 = cleaner.CleanText(commaTextLine1);        //Header
            Assert.IsTrue(result1);

            var resultText1 = cleaner.ReturnText();
            Assert.AreEqual<string>(commaTextLine1, resultText1);

            var result2 = cleaner.CleanText(commaTextLine2StrayTextQuote);        //Data
            Assert.IsFalse(result2);

            var resultText2 = cleaner.ReturnText();                 //More data
            Assert.AreEqual<string>(commaTextLine2StrayTextQuoteReturned, resultText2);
        }

        [TestMethod()]
        public void ReturnHeadersAndFieldsTest()
        {
            Cleaner cleaner = new Cleaner();
            Assert.IsNotNull(cleaner);

            var result1 = cleaner.CleanText(commaTextLine1);        //Header
            Assert.IsTrue(result1);

            var resultText1 = cleaner.ReturnHeaders();
            CollectionAssert.AreEqual(textLine1Array, resultText1);

            var result2 = cleaner.CleanText(commaTextLine2);        //Simple data
            Assert.IsTrue(result2);

            var resultText2 = cleaner.ReturnFields();               
            CollectionAssert.AreEqual(textLine2Array, resultText2);

            var result3 = cleaner.CleanText(commaTextLine3);        //Trailing field delim
            Assert.IsTrue(result3);

            var resultText3 = cleaner.ReturnFields();               
            CollectionAssert.AreEqual(textLine3Array, resultText3);

            var result4 = cleaner.CleanText(commaTextLine4);        //Quoted EOL
            Assert.IsTrue(result4);

            var resultText4 = cleaner.ReturnFields();               
            CollectionAssert.AreEqual(textLine4Array, resultText4);
        }

        [TestMethod()]
        public void IndexerTest()
        {
            Cleaner cleaner = new Cleaner();
            Assert.IsNotNull(cleaner);

            var result1 = cleaner.CleanText(commaTextLine1);        //Header
            Assert.IsTrue(result1);

            var second = cleaner["Second"];
            Assert.AreEqual(second, "Second");

            var fourth = cleaner["Fourth"];
            Assert.AreEqual(fourth, "Fourth");

            var zeroeth = cleaner["Zeroeth"];
            Assert.IsNull(zeroeth);

            var third = cleaner["Third"];
            Assert.AreEqual(third, "Third");

            var first = cleaner["First"];
            Assert.AreEqual(first, "First");
        }
    }
}