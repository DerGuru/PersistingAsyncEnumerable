using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PersistingEnumerableTestst
{
    [TestClass]
    public class PersistingEnumeratorTests
    {

        public async IAsyncEnumerable<int> GetAsyncEnuDelayed(int count, int delay)
        {
            int i = 0;
            while (i < count)
            {
                await Task.Delay(delay);
                yield return i;
                ++i;
            }
        }

        [TestMethod]
        public async Task AsyncTest() 
        {
            await using (var p = new SingleThreadPersistingAsyncEnumerable<int>(GetAsyncEnuDelayed(100, 5)))
            {
                List<int> first = new List<int>(100);

                Stopwatch s1 = Stopwatch.StartNew();
                await foreach (int i in p)
                {
                    first.Add(i);
                }
                s1.Stop();
                List<int> second = new List<int>(100);

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

            using (var p = new SingleThreadPersistingAsyncEnumerable<int>(GetAsyncEnuDelayed(100, 5)))
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

        private IEnumerable<Task<T>> GetWorkers<T>(int count, Func<Task<T>> func, object locker)
        {
                for (int i = 0; i < count; ++i)
                    yield return func();
        }

        [TestMethod]
        public async Task ParallelTest()
        {
            int count = 10000;
            var locker = new object();
            await using (var p = new ThreadSafePersistingAsyncEnumerable<int>(GetAsyncEnuDelayed(count, 0)))
            {
                var workers = GetWorkers(20, async () =>
                {
                    List<int> list = new List<int>(count);
                    await foreach (int i in p)
                    {
                        list.Add(i);
                    }
                    return list;
                }, locker).ToList();
                var results = (await Task.WhenAll(workers)).ToList();

                Assert.IsTrue(results.Select(x => x.Count).All(x => x == count), new StringBuilder().AppendJoin(',',results.Select(x => x.Count)).ToString());
                for (int i = 0; i < count; ++ i)
                {
                    Assert.IsTrue(results.Select(x => x[i]).All(x => x == i), $"{i}");
                }

            }
        }

        [TestMethod]
        public async Task AutoLoadingParallelTest()
        {
            int count = 1000;
            var locker = new object();
            await using (var p = new AutoLoadingPersistingAsyncEnumerable<int>(GetAsyncEnuDelayed(count, 1)))
            {
                var workers = GetWorkers(5, async () =>
                {
                    List<int> list = new List<int>(count);
                    await foreach (int i in p)
                    {
                        list.Add(i);
                    }
                    return list;
                }, locker).ToList();
                var results = (await Task.WhenAll(workers)).ToList();

                Assert.IsTrue(results.Select(x => x.Count).All(x => x == count), new StringBuilder().AppendJoin(',', results.Select(x => x.Count)).ToString());
                for (int i = 0; i < count; ++i)
                {
                    Assert.IsTrue(results.Select(x => x[i]).All(x => x == i), $"{i}");
                }

            }
        }


        [TestMethod]
        public async Task LinqTest()
        {
            var p = new ThreadSafePersistingAsyncEnumerable<int>(GetAsyncEnuDelayed(100, 500));
            Task t1 = new Task(() => Assert.IsTrue(p.Contains(5)));
            Task t2 = new Task(() => Assert.IsTrue(p.Contains(50)));
            Task t3 = new Task(() => {
                var odds = p.Where(x => x % 2 > 0);
                Assert.IsTrue(odds.Count() == 50);
            });
            var tasks = new List<Task> { t1, t2, t3 };
            tasks.ForEach(x => x.Start());
            await Task.WhenAll(tasks);
        }
    }
}
