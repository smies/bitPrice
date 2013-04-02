
namespace orderBookTest
open System
open NUnit.Framework

[<TestFixture>]
type Test() = 
        [<Test>]
        member this.TestCase  () =
            ()
        [<Test>]
        member this.ModTest  () =
            Assert.AreEqual(2, orderBook.Internal.UtilityModule.utilityFunction ())
