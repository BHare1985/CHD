using NUnit.Framework;

namespace CHD.Tests;

public class Tests
{
    [Test]
    [TestCase(0.5)]
    [TestCase(1)]
    public void SmokeTest(double loadFactor)
    {
        var keys = new List<byte[]>();
        var numKeys = (int)Math.Pow(2, 12);
        for (var i = 0; i < numKeys; i++)
            keys.Add(BitConverter.GetBytes(i));

        var hashFunc = new PerfectHashFunction(loadFactor: loadFactor);
        var dump = hashFunc.Construct(keys);

        var ids = keys.Select(x => hashFunc.Hash(x)).Distinct().ToArray();
        Assert.That(keys, Has.Count.EqualTo(ids.Length));

        var importedHashFunc = new PerfectHashFunction();
        importedHashFunc.Import(keys.Count, dump, loadFactor);

        var ids2 = keys.Select(x => importedHashFunc.Hash(x)).Distinct().ToArray();
        Assert.That(keys, Has.Count.EqualTo(ids2.Length));

        CollectionAssert.AreEqual(ids, ids2);
    }
}