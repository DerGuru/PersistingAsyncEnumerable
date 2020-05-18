using Microsoft.VisualStudio.TestTools.UnitTesting;
using PersistingAsyncEnumerable;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PersistingEnumerableTestst
{
    [TestClass]
    public class PersistingEnumeratorTests
    {

        public async IAsyncEnumerable<int> GetAsyncEnu(int count)
        {
            int i = 0;
            while (i < count)
            {
                yield return await Task.Run(() => i);
                ++i;
            }
        }
        public async IAsyncEnumerable<int> GetAsyncEnuDelayed(int count, int delay)
        {
            int i = 0;
            while (i < count)
            {
                await Task.Delay(delay);
                yield return await Task.Run(() => i);
                ++i;
            }
        }

        [TestMethod]
        public async Task AsyncTest()
        {
            await using (var p = new PersistingAsyncEnumerable<int>(GetAsyncEnuDelayed(1000,50)))
            {
                List<int> first = new List<int>(10000);

                Stopwatch s1 = Stopwatch.StartNew();
                await foreach (int i in p)
                {
                    first.Add(i);
                }
                s1.Stop();
                List<int> second = new List<int>(10000);

                Stopwatch s2 = Stopwatch.StartNew();
                await foreach (int i in p)
                {
                    second.Add(i);
                }
                s2.Stop();

                var ass = first.Zip(second);
                foreach (var a in ass)
                {
                    Assert.AreEqual(a.First, a.Second);
                }
                Assert.IsTrue(s1.Elapsed > s2.Elapsed);
            }
        }

        [TestMethod]
        public void SyncTest()
        {

            using (var p = new PersistingAsyncEnumerable<int>(GetAsyncEnu(10000)))
            {
                Stopwatch s1 = Stopwatch.StartNew();
                List<int> first = p.ToList();
                s1.Stop();

                Stopwatch s2 = Stopwatch.StartNew();
                List<int> second = p.ToList();
                s2.Stop();

                var ass = first.Zip(second);
                foreach (var a in ass)
                {
                    Assert.AreEqual(a.First, a.Second);
                }
                Assert.IsTrue(s1.Elapsed > s2.Elapsed);
            }
        }

        [TestMethod]
        public async Task ParallelTest()
        {
            await using (var p = new PersistingAsyncEnumerable<int>(GetAsyncEnu(100000)))
            {
                List<int> l1 = new List<int>(100000);

                var t1 = Task.Run(async () =>
                {
                    await foreach (int i in p)
                    {
                        l1.Add(i);
                    }
                });
                List<int> l2 = new List<int>(100000);

                var t2 = Task.Run(async () =>
                {
                    await foreach (int i in p)
                    {
                        l2.Add(i);
                    }
                });
                List<int> l3 = new List<int>(100000);

                var t3 = Task.Run(async () =>
                {
                    await foreach (int i in p)
                    {
                        l3.Add(i);
                    }
                }); List<int> l4 = new List<int>(100000);

                var t4 = Task.Run(async () =>
                {
                    await Task.Delay(30);
                    await foreach (int i in p)
                    {
                        l4.Add(i);
                    }
                });
                List<int> l5 = new List<int>(100000);

                var t5 = Task.Run(async () =>
                {
                    await foreach (int i in p)
                    {
                        l5.Add(i);
                    }
                });

                await Task.WhenAll(t1, t2, t3, t4, t5);

                var ass = l1.Zip(l2);
                foreach (var a in ass)
                {
                    Assert.AreEqual(a.First, a.Second);
                }
            }
        }
    }
}
