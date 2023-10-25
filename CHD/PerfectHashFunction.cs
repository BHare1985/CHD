namespace CHD;

public class PerfectHashFunction
{
    private const uint InitSeed = 0;
    private const int SignMask = 0xfffffff;

    private readonly int _keysPerBucket;
    private readonly double _loadFactor;
    private readonly int _maxSeed;

    private uint[] _displacementArray = Array.Empty<uint>();
    private int _numBins;

    private int _numBuckets;
    private int _numKeys;

    public PerfectHashFunction(int keysPerBucket = 4, int maxSeed = int.MaxValue, double loadFactor = 1.0)
    {
        if (loadFactor > 1.0)
            throw new ArgumentException("Load factor should be <= 1.0");

        _keysPerBucket = keysPerBucket;
        _maxSeed = maxSeed;
        _loadFactor = loadFactor;
    }

    public IEnumerable<uint> Construct(IList<byte[]> keys)
    {
        _numKeys = keys.Count;
        _numBins = (int)(_numKeys / _loadFactor);
        _numBuckets = _numKeys / _keysPerBucket;
        _displacementArray = new uint[_numBuckets];

        var processedArray = new bool[_numBins];
        var buckets = CreateBuckets(keys);

        foreach (var bucket in buckets)
        {
            if (bucket.Count == 0) continue;

            var kArray = new HashSet<uint>(bucket.Count);
            var processed = Enumerable.Range(0, processedArray.Length)
                .Select(x => (uint)x)
                .Where(j => processedArray[j])
                .ToHashSet();

            var success = false;
            for (var displacement = InitSeed + 1; success == false && displacement < _maxSeed; displacement++)
            {
                if (bucket.Any(key => !kArray.Add(Phi(key, displacement))))
                    kArray.Clear();

                if (kArray.Count != bucket.Count || kArray.Intersect(processed).Any())
                    continue;

                success = true;
                _displacementArray[GetBucketIndex(bucket.First())] = displacement;
                foreach (var j in kArray)
                    processedArray[j] = true;
            }

            if (success == false)
                throw new Exception("Cannot construct perfect hash function");
        }

        return _displacementArray;
    }

    public uint Hash(IList<byte> key)
    {
        var displacement = _displacementArray[GetBucketIndex(key)];
        return Phi(key, displacement);
    }

    public void Import(int keySize, IEnumerable<uint> displacementArray, double loadFactor = 1.0)
    {
        _numKeys = keySize;
        _numBins = (int)(_numKeys / loadFactor);
        _displacementArray = displacementArray.ToArray();
        _numBuckets = _displacementArray.Length;
    }

    private IEnumerable<List<byte[]>> CreateBuckets(IEnumerable<byte[]> keys)
    {
        var buckets = new List<byte[]>[_numBuckets];
        for (var i = 0; i < _numBuckets; i++)
            buckets[i] = new List<byte[]>();

        foreach (var key in keys)
            buckets[GetBucketIndex(key)].Add(key);

        return buckets.OrderByDescending(bucket => bucket.Count);
    }

    private uint GetBucketIndex(IEnumerable<byte> key)
    {
        return Hash(key, InitSeed, _numBuckets);
    }

    private uint Phi(IEnumerable<byte> keyByte, uint displacement)
    {
        return Hash(keyByte, displacement, _numBins);
    }

    private static uint Hash(IEnumerable<byte> key, uint seed, int mod)
    {
        ReadOnlySpan<byte> byteSpan = key.ToArray().AsSpan();
        var hash = MurmurHash3.Hash32(ref byteSpan, seed) & SignMask;

        return (uint)(hash % mod);
    }
}