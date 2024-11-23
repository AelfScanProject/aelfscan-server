using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace AElfScanServer.Mocks;

public class LocalCacheDatabase : IDatabase
{
    private readonly Dictionary<string, RedisValue> _cache = new Dictionary<string, RedisValue>();
    private IDatabase _databaseImplementation;

    public double StringDecrement(RedisKey key, double value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringDecrement(key, value, flags);
    }

    public RedisValue StringGet(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        _cache.TryGetValue(key, out var value);
        return value;
    }

    public RedisValue[] StringGet(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringGet(keys, flags);
    }

    public Lease<byte>? StringGetLease(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringGetLease(key, flags);
    }

    public bool StringGetBit(RedisKey key, long offset, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringGetBit(key, offset, flags);
    }

    public RedisValue StringGetRange(RedisKey key, long start, long end, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringGetRange(key, start, end, flags);
    }

    public RedisValue StringGetSet(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringGetSet(key, value, flags);
    }

    public RedisValue StringGetSetExpiry(RedisKey key, TimeSpan? expiry, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringGetSetExpiry(key, expiry, flags);
    }

    public RedisValue StringGetSetExpiry(RedisKey key, DateTime expiry, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringGetSetExpiry(key, expiry, flags);
    }

    public RedisValue StringGetDelete(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringGetDelete(key, flags);
    }

    public RedisValueWithExpiry StringGetWithExpiry(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringGetWithExpiry(key, flags);
    }

    public Task<double> StringDecrementAsync(RedisKey key, double value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringDecrementAsync(key, value, flags);
    }

    public Task<RedisValue> StringGetAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return Task.FromResult(StringGet(key, flags));
    }

    public Task<RedisValue[]> StringGetAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringGetAsync(keys, flags);
    }

    public Task<Lease<byte>?> StringGetLeaseAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringGetLeaseAsync(key, flags);
    }

    public Task<bool> StringGetBitAsync(RedisKey key, long offset, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringGetBitAsync(key, offset, flags);
    }

    public Task<RedisValue> StringGetRangeAsync(RedisKey key, long start, long end, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringGetRangeAsync(key, start, end, flags);
    }

    public Task<RedisValue> StringGetSetAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringGetSetAsync(key, value, flags);
    }

    public Task<RedisValue> StringGetSetExpiryAsync(RedisKey key, TimeSpan? expiry, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringGetSetExpiryAsync(key, expiry, flags);
    }

    public Task<RedisValue> StringGetSetExpiryAsync(RedisKey key, DateTime expiry, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringGetSetExpiryAsync(key, expiry, flags);
    }

    public Task<RedisValue> StringGetDeleteAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringGetDeleteAsync(key, flags);
    }

    public Task<RedisValueWithExpiry> StringGetWithExpiryAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringGetWithExpiryAsync(key, flags);
    }

    public bool StringSet(RedisKey key, RedisValue value, TimeSpan? expiry, When when)
    {
        return _databaseImplementation.StringSet(key, value, expiry, when);
    }

    public bool StringSet(RedisKey key, RedisValue value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None)
    {
        _cache[key] = value;
        return true;
    }

    public bool StringSet(RedisKey key, RedisValue value, TimeSpan? expiry = null, bool keepTtl = false, When when = When.Always,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringSet(key, value, expiry, keepTtl, when, flags);
    }

    public bool StringSet(KeyValuePair<RedisKey, RedisValue>[] values, When when = When.Always, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringSet(values, when, flags);
    }

    public RedisValue StringSetAndGet(RedisKey key, RedisValue value, TimeSpan? expiry, When when, CommandFlags flags)
    {
        return _databaseImplementation.StringSetAndGet(key, value, expiry, when, flags);
    }

    public RedisValue StringSetAndGet(RedisKey key, RedisValue value, TimeSpan? expiry = null, bool keepTtl = false,
        When when = When.Always, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringSetAndGet(key, value, expiry, keepTtl, when, flags);
    }

    public bool StringSetBit(RedisKey key, long offset, bool bit, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringSetBit(key, offset, bit, flags);
    }

    public RedisValue StringSetRange(RedisKey key, long offset, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringSetRange(key, offset, value, flags);
    }

    public int Database => _databaseImplementation.Database;

    public Task<bool> StringSetAsync(RedisKey key, RedisValue value, TimeSpan? expiry, When when)
    {
        return _databaseImplementation.StringSetAsync(key, value, expiry, when);
    }

    public Task<bool> StringSetAsync(RedisKey key, RedisValue value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None)
    {
        return Task.FromResult(StringSet(key, value, expiry, when, flags));
    }

    public Task<bool> StringSetAsync(RedisKey key, RedisValue value, TimeSpan? expiry = null, bool keepTtl = false, When when = When.Always,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringSetAsync(key, value, expiry, keepTtl, when, flags);
    }

    public Task<bool> StringSetAsync(KeyValuePair<RedisKey, RedisValue>[] values, When when = When.Always, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringSetAsync(values, when, flags);
    }

    public Task<RedisValue> StringSetAndGetAsync(RedisKey key, RedisValue value, TimeSpan? expiry, When when, CommandFlags flags)
    {
        return _databaseImplementation.StringSetAndGetAsync(key, value, expiry, when, flags);
    }

    public Task<RedisValue> StringSetAndGetAsync(RedisKey key, RedisValue value, TimeSpan? expiry = null, bool keepTtl = false,
        When when = When.Always, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringSetAndGetAsync(key, value, expiry, keepTtl, when, flags);
    }

    public Task<bool> StringSetBitAsync(RedisKey key, long offset, bool bit, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringSetBitAsync(key, offset, bit, flags);
    }

    public Task<RedisValue> StringSetRangeAsync(RedisKey key, long offset, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringSetRangeAsync(key, offset, value, flags);
    }

    // Implement other methods as needed...
    
    // For example: Remove, Increment, Decrement, etc.

    public long StringIncrement(RedisKey key, long value = 1, CommandFlags flags = CommandFlags.None)
    {
        if (!_cache.ContainsKey(key))
        {
            _cache[key] = value;
        }
        else
        {
            _cache[key] = (long)_cache[key] + value;
        }
        return (long)_cache[key];
    }

    public double StringIncrement(RedisKey key, double value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringIncrement(key, value, flags);
    }

    public long StringLength(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringLength(key, flags);
    }

    public string? StringLongestCommonSubsequence(RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringLongestCommonSubsequence(first, second, flags);
    }

    public long StringLongestCommonSubsequenceLength(RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringLongestCommonSubsequenceLength(first, second, flags);
    }

    public LCSMatchResult StringLongestCommonSubsequenceWithMatches(RedisKey first, RedisKey second, long minLength = 0,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringLongestCommonSubsequenceWithMatches(first, second, minLength, flags);
    }

    // Others method placeholders
    public Task<long> StringIncrementAsync(RedisKey key, long value = 1, CommandFlags flags = CommandFlags.None)
    {
        return Task.FromResult(StringIncrement(key, value, flags));
    }

    public Task<double> StringIncrementAsync(RedisKey key, double value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringIncrementAsync(key, value, flags);
    }

    public Task<long> StringLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringLengthAsync(key, flags);
    }

    public Task<string?> StringLongestCommonSubsequenceAsync(RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringLongestCommonSubsequenceAsync(first, second, flags);
    }

    public Task<long> StringLongestCommonSubsequenceLengthAsync(RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringLongestCommonSubsequenceLengthAsync(first, second, flags);
    }

    public Task<LCSMatchResult> StringLongestCommonSubsequenceWithMatchesAsync(RedisKey first, RedisKey second, long minLength = 0,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringLongestCommonSubsequenceWithMatchesAsync(first, second, minLength, flags);
    }

    public IBatch CreateBatch(object? asyncState = null)
    {
        return _databaseImplementation.CreateBatch(asyncState);
    }

    public ITransaction CreateTransaction(object? asyncState = null)
    {
        return _databaseImplementation.CreateTransaction(asyncState);
    }

    public void KeyMigrate(RedisKey key, EndPoint toServer, int toDatabase = 0, int timeoutMilliseconds = 0,
        MigrateOptions migrateOptions = MigrateOptions.None, CommandFlags flags = CommandFlags.None)
    {
        _databaseImplementation.KeyMigrate(key, toServer, toDatabase, timeoutMilliseconds, migrateOptions, flags);
    }

    public RedisValue DebugObject(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.DebugObject(key, flags);
    }

    public bool GeoAdd(RedisKey key, double longitude, double latitude, RedisValue member, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoAdd(key, longitude, latitude, member, flags);
    }

    public bool GeoAdd(RedisKey key, GeoEntry value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoAdd(key, value, flags);
    }

    public long GeoAdd(RedisKey key, GeoEntry[] values, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoAdd(key, values, flags);
    }

    public bool GeoRemove(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoRemove(key, member, flags);
    }

    public double? GeoDistance(RedisKey key, RedisValue member1, RedisValue member2, GeoUnit unit = GeoUnit.Meters,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoDistance(key, member1, member2, unit, flags);
    }

    public string?[] GeoHash(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoHash(key, members, flags);
    }

    public string? GeoHash(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoHash(key, member, flags);
    }

    public GeoPosition?[] GeoPosition(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoPosition(key, members, flags);
    }

    public GeoPosition? GeoPosition(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoPosition(key, member, flags);
    }

    public GeoRadiusResult[] GeoRadius(RedisKey key, RedisValue member, double radius, GeoUnit unit = GeoUnit.Meters, int count = -1,
        Order? order = null, GeoRadiusOptions options = GeoRadiusOptions.Default, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoRadius(key, member, radius, unit, count, order, options, flags);
    }

    public GeoRadiusResult[] GeoRadius(RedisKey key, double longitude, double latitude, double radius, GeoUnit unit = GeoUnit.Meters,
        int count = -1, Order? order = null, GeoRadiusOptions options = GeoRadiusOptions.Default, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoRadius(key, longitude, latitude, radius, unit, count, order, options, flags);
    }

    public GeoRadiusResult[] GeoSearch(RedisKey key, RedisValue member, GeoSearchShape shape, int count = -1,
        bool demandClosest = true, Order? order = null, GeoRadiusOptions options = GeoRadiusOptions.Default, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoSearch(key, member, shape, count, demandClosest, order, options, flags);
    }

    public GeoRadiusResult[] GeoSearch(RedisKey key, double longitude, double latitude, GeoSearchShape shape, int count = -1,
        bool demandClosest = true, Order? order = null, GeoRadiusOptions options = GeoRadiusOptions.Default, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoSearch(key, longitude, latitude, shape, count, demandClosest, order, options, flags);
    }

    public long GeoSearchAndStore(RedisKey sourceKey, RedisKey destinationKey, RedisValue member, GeoSearchShape shape,
        int count = -1, bool demandClosest = true, Order? order = null, bool storeDistances = false,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoSearchAndStore(sourceKey, destinationKey, member, shape, count, demandClosest, order, storeDistances, flags);
    }

    public long GeoSearchAndStore(RedisKey sourceKey, RedisKey destinationKey, double longitude, double latitude,
        GeoSearchShape shape, int count = -1, bool demandClosest = true, Order? order = null, bool storeDistances = false,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoSearchAndStore(sourceKey, destinationKey, longitude, latitude, shape, count, demandClosest, order, storeDistances, flags);
    }

    public long HashDecrement(RedisKey key, RedisValue hashField, long value = 1, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashDecrement(key, hashField, value, flags);
    }

    public double HashDecrement(RedisKey key, RedisValue hashField, double value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashDecrement(key, hashField, value, flags);
    }

    public bool HashDelete(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashDelete(key, hashField, flags);
    }

    public long HashDelete(RedisKey key, RedisValue[] hashFields, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashDelete(key, hashFields, flags);
    }

    public bool HashExists(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashExists(key, hashField, flags);
    }

    public RedisValue HashGet(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashGet(key, hashField, flags);
    }

    public Lease<byte>? HashGetLease(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashGetLease(key, hashField, flags);
    }

    public RedisValue[] HashGet(RedisKey key, RedisValue[] hashFields, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashGet(key, hashFields, flags);
    }

    public HashEntry[] HashGetAll(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashGetAll(key, flags);
    }

    public long HashIncrement(RedisKey key, RedisValue hashField, long value = 1, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashIncrement(key, hashField, value, flags);
    }

    public double HashIncrement(RedisKey key, RedisValue hashField, double value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashIncrement(key, hashField, value, flags);
    }

    public RedisValue[] HashKeys(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashKeys(key, flags);
    }

    public long HashLength(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashLength(key, flags);
    }

    public RedisValue HashRandomField(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashRandomField(key, flags);
    }

    public RedisValue[] HashRandomFields(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashRandomFields(key, count, flags);
    }

    public HashEntry[] HashRandomFieldsWithValues(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashRandomFieldsWithValues(key, count, flags);
    }

    public IEnumerable<HashEntry> HashScan(RedisKey key, RedisValue pattern, int pageSize, CommandFlags flags)
    {
        return _databaseImplementation.HashScan(key, pattern, pageSize, flags);
    }

    public IEnumerable<HashEntry> HashScan(RedisKey key, RedisValue pattern = new RedisValue(), int pageSize = 250, long cursor = 0,
        int pageOffset = 0, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashScan(key, pattern, pageSize, cursor, pageOffset, flags);
    }

    public void HashSet(RedisKey key, HashEntry[] hashFields, CommandFlags flags = CommandFlags.None)
    {
        _databaseImplementation.HashSet(key, hashFields, flags);
    }

    public bool HashSet(RedisKey key, RedisValue hashField, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashSet(key, hashField, value, when, flags);
    }

    public long HashStringLength(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashStringLength(key, hashField, flags);
    }

    public RedisValue[] HashValues(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashValues(key, flags);
    }

    public bool HyperLogLogAdd(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HyperLogLogAdd(key, value, flags);
    }

    public bool HyperLogLogAdd(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HyperLogLogAdd(key, values, flags);
    }

    public long HyperLogLogLength(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HyperLogLogLength(key, flags);
    }

    public long HyperLogLogLength(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HyperLogLogLength(keys, flags);
    }

    public void HyperLogLogMerge(RedisKey destination, RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
    {
        _databaseImplementation.HyperLogLogMerge(destination, first, second, flags);
    }

    public void HyperLogLogMerge(RedisKey destination, RedisKey[] sourceKeys, CommandFlags flags = CommandFlags.None)
    {
        _databaseImplementation.HyperLogLogMerge(destination, sourceKeys, flags);
    }

    public EndPoint? IdentifyEndpoint(RedisKey key = new RedisKey(), CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.IdentifyEndpoint(key, flags);
    }

    public bool KeyCopy(RedisKey sourceKey, RedisKey destinationKey, int destinationDatabase = -1, bool replace = false,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyCopy(sourceKey, destinationKey, destinationDatabase, replace, flags);
    }

    public bool KeyDelete(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyDelete(key, flags);
    }

    public long KeyDelete(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyDelete(keys, flags);
    }

    public byte[]? KeyDump(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyDump(key, flags);
    }

    public string? KeyEncoding(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyEncoding(key, flags);
    }

    public bool KeyExists(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _cache.ContainsKey(key);
    }

    public long KeyExists(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyExists(keys, flags);
    }

    public bool KeyExpire(RedisKey key, TimeSpan? expiry, CommandFlags flags)
    {
        return _databaseImplementation.KeyExpire(key, expiry, flags);
    }

    public bool KeyExpire(RedisKey key, TimeSpan? expiry, ExpireWhen when = ExpireWhen.Always, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyExpire(key, expiry, when, flags);
    }

    public bool KeyExpire(RedisKey key, DateTime? expiry, CommandFlags flags)
    {
        return _databaseImplementation.KeyExpire(key, expiry, flags);
    }

    public bool KeyExpire(RedisKey key, DateTime? expiry, ExpireWhen when = ExpireWhen.Always, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyExpire(key, expiry, when, flags);
    }

    public DateTime? KeyExpireTime(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyExpireTime(key, flags);
    }

    public long? KeyFrequency(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyFrequency(key, flags);
    }

    public TimeSpan? KeyIdleTime(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyIdleTime(key, flags);
    }

    public bool KeyMove(RedisKey key, int database, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyMove(key, database, flags);
    }

    public bool KeyPersist(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyPersist(key, flags);
    }

    public RedisKey KeyRandom(CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyRandom(flags);
    }

    public long? KeyRefCount(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyRefCount(key, flags);
    }

    public bool KeyRename(RedisKey key, RedisKey newKey, When when = When.Always, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyRename(key, newKey, when, flags);
    }

    public void KeyRestore(RedisKey key, byte[] value, TimeSpan? expiry = null, CommandFlags flags = CommandFlags.None)
    {
        _databaseImplementation.KeyRestore(key, value, expiry, flags);
    }

    public TimeSpan? KeyTimeToLive(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyTimeToLive(key, flags);
    }

    public bool KeyTouch(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyTouch(key, flags);
    }

    public long KeyTouch(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyTouch(keys, flags);
    }

    public RedisType KeyType(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyType(key, flags);
    }

    public RedisValue ListGetByIndex(RedisKey key, long index, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListGetByIndex(key, index, flags);
    }

    public long ListInsertAfter(RedisKey key, RedisValue pivot, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListInsertAfter(key, pivot, value, flags);
    }

    public long ListInsertBefore(RedisKey key, RedisValue pivot, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListInsertBefore(key, pivot, value, flags);
    }

    public RedisValue ListLeftPop(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListLeftPop(key, flags);
    }

    public RedisValue[] ListLeftPop(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListLeftPop(key, count, flags);
    }

    public ListPopResult ListLeftPop(RedisKey[] keys, long count, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListLeftPop(keys, count, flags);
    }

    public long ListPosition(RedisKey key, RedisValue element, long rank = 1, long maxLength = 0, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListPosition(key, element, rank, maxLength, flags);
    }

    public long[] ListPositions(RedisKey key, RedisValue element, long count, long rank = 1, long maxLength = 0,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListPositions(key, element, count, rank, maxLength, flags);
    }

    public long ListLeftPush(RedisKey key, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListLeftPush(key, value, when, flags);
    }

    public long ListLeftPush(RedisKey key, RedisValue[] values, When when = When.Always, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListLeftPush(key, values, when, flags);
    }

    public long ListLeftPush(RedisKey key, RedisValue[] values, CommandFlags flags)
    {
        return _databaseImplementation.ListLeftPush(key, values, flags);
    }

    public long ListLength(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListLength(key, flags);
    }

    public RedisValue ListMove(RedisKey sourceKey, RedisKey destinationKey, ListSide sourceSide, ListSide destinationSide,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListMove(sourceKey, destinationKey, sourceSide, destinationSide, flags);
    }

    public RedisValue[] ListRange(RedisKey key, long start = 0, long stop = -1, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListRange(key, start, stop, flags);
    }

    public long ListRemove(RedisKey key, RedisValue value, long count = 0, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListRemove(key, value, count, flags);
    }

    public RedisValue ListRightPop(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListRightPop(key, flags);
    }

    public RedisValue[] ListRightPop(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListRightPop(key, count, flags);
    }

    public ListPopResult ListRightPop(RedisKey[] keys, long count, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListRightPop(keys, count, flags);
    }

    public RedisValue ListRightPopLeftPush(RedisKey source, RedisKey destination, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListRightPopLeftPush(source, destination, flags);
    }

    public long ListRightPush(RedisKey key, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListRightPush(key, value, when, flags);
    }

    public long ListRightPush(RedisKey key, RedisValue[] values, When when = When.Always, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListRightPush(key, values, when, flags);
    }

    public long ListRightPush(RedisKey key, RedisValue[] values, CommandFlags flags)
    {
        return _databaseImplementation.ListRightPush(key, values, flags);
    }

    public void ListSetByIndex(RedisKey key, long index, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        _databaseImplementation.ListSetByIndex(key, index, value, flags);
    }

    public void ListTrim(RedisKey key, long start, long stop, CommandFlags flags = CommandFlags.None)
    {
        _databaseImplementation.ListTrim(key, start, stop, flags);
    }

    public bool LockExtend(RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.LockExtend(key, value, expiry, flags);
    }

    public RedisValue LockQuery(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.LockQuery(key, flags);
    }

    public bool LockRelease(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.LockRelease(key, value, flags);
    }

    public bool LockTake(RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.LockTake(key, value, expiry, flags);
    }

    public long Publish(RedisChannel channel, RedisValue message, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.Publish(channel, message, flags);
    }

    public RedisResult Execute(string command, params object[] args)
    {
        return _databaseImplementation.Execute(command, args);
    }

    public RedisResult Execute(string command, ICollection<object> args, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.Execute(command, args, flags);
    }

    public RedisResult ScriptEvaluate(string script, RedisKey[]? keys = null, RedisValue[]? values = null,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ScriptEvaluate(script, keys, values, flags);
    }

    public RedisResult ScriptEvaluate(byte[] hash, RedisKey[]? keys = null, RedisValue[]? values = null,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ScriptEvaluate(hash, keys, values, flags);
    }

    public RedisResult ScriptEvaluate(LuaScript script, object? parameters = null, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ScriptEvaluate(script, parameters, flags);
    }

    public RedisResult ScriptEvaluate(LoadedLuaScript script, object? parameters = null, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ScriptEvaluate(script, parameters, flags);
    }

    public RedisResult ScriptEvaluateReadOnly(string script, RedisKey[]? keys = null, RedisValue[]? values = null,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ScriptEvaluateReadOnly(script, keys, values, flags);
    }

    public RedisResult ScriptEvaluateReadOnly(byte[] hash, RedisKey[]? keys = null, RedisValue[]? values = null,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ScriptEvaluateReadOnly(hash, keys, values, flags);
    }

    public bool SetAdd(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetAdd(key, value, flags);
    }

    public long SetAdd(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetAdd(key, values, flags);
    }

    public RedisValue[] SetCombine(SetOperation operation, RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetCombine(operation, first, second, flags);
    }

    public RedisValue[] SetCombine(SetOperation operation, RedisKey[] keys, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetCombine(operation, keys, flags);
    }

    public long SetCombineAndStore(SetOperation operation, RedisKey destination, RedisKey first, RedisKey second,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetCombineAndStore(operation, destination, first, second, flags);
    }

    public long SetCombineAndStore(SetOperation operation, RedisKey destination, RedisKey[] keys, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetCombineAndStore(operation, destination, keys, flags);
    }

    public bool SetContains(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetContains(key, value, flags);
    }

    public bool[] SetContains(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetContains(key, values, flags);
    }

    public long SetIntersectionLength(RedisKey[] keys, long limit = 0, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetIntersectionLength(keys, limit, flags);
    }

    public long SetLength(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetLength(key, flags);
    }

    public RedisValue[] SetMembers(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetMembers(key, flags);
    }

    public bool SetMove(RedisKey source, RedisKey destination, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetMove(source, destination, value, flags);
    }

    public RedisValue SetPop(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetPop(key, flags);
    }

    public RedisValue[] SetPop(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetPop(key, count, flags);
    }

    public RedisValue SetRandomMember(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetRandomMember(key, flags);
    }

    public RedisValue[] SetRandomMembers(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetRandomMembers(key, count, flags);
    }

    public bool SetRemove(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetRemove(key, value, flags);
    }

    public long SetRemove(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetRemove(key, values, flags);
    }

    public IEnumerable<RedisValue> SetScan(RedisKey key, RedisValue pattern, int pageSize, CommandFlags flags)
    {
        return _databaseImplementation.SetScan(key, pattern, pageSize, flags);
    }

    public IEnumerable<RedisValue> SetScan(RedisKey key, RedisValue pattern = new RedisValue(), int pageSize = 250, long cursor = 0,
        int pageOffset = 0, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetScan(key, pattern, pageSize, cursor, pageOffset, flags);
    }

    public RedisValue[] Sort(RedisKey key, long skip = 0, long take = -1, Order order = Order.Ascending, SortType sortType = SortType.Numeric,
        RedisValue by = new RedisValue(), RedisValue[]? get = null, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.Sort(key, skip, take, order, sortType, by, get, flags);
    }

    public long SortAndStore(RedisKey destination, RedisKey key, long skip = 0, long take = -1, Order order = Order.Ascending,
        SortType sortType = SortType.Numeric, RedisValue by = new RedisValue(), RedisValue[]? get = null, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortAndStore(destination, key, skip, take, order, sortType, by, get, flags);
    }

    public bool SortedSetAdd(RedisKey key, RedisValue member, double score, CommandFlags flags)
    {
        return _databaseImplementation.SortedSetAdd(key, member, score, flags);
    }

    public bool SortedSetAdd(RedisKey key, RedisValue member, double score, When when, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetAdd(key, member, score, when, flags);
    }

    public bool SortedSetAdd(RedisKey key, RedisValue member, double score, SortedSetWhen when = SortedSetWhen.Always,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetAdd(key, member, score, when, flags);
    }

    public long SortedSetAdd(RedisKey key, SortedSetEntry[] values, CommandFlags flags)
    {
        return _databaseImplementation.SortedSetAdd(key, values, flags);
    }

    public long SortedSetAdd(RedisKey key, SortedSetEntry[] values, When when, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetAdd(key, values, when, flags);
    }

    public long SortedSetAdd(RedisKey key, SortedSetEntry[] values, SortedSetWhen when = SortedSetWhen.Always, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetAdd(key, values, when, flags);
    }

    public RedisValue[] SortedSetCombine(SetOperation operation, RedisKey[] keys, double[]? weights = null,
        Aggregate aggregate = Aggregate.Sum, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetCombine(operation, keys, weights, aggregate, flags);
    }

    public SortedSetEntry[] SortedSetCombineWithScores(SetOperation operation, RedisKey[] keys, double[]? weights = null,
        Aggregate aggregate = Aggregate.Sum, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetCombineWithScores(operation, keys, weights, aggregate, flags);
    }

    public long SortedSetCombineAndStore(SetOperation operation, RedisKey destination, RedisKey first, RedisKey second,
        Aggregate aggregate = Aggregate.Sum, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetCombineAndStore(operation, destination, first, second, aggregate, flags);
    }

    public long SortedSetCombineAndStore(SetOperation operation, RedisKey destination, RedisKey[] keys, double[]? weights = null,
        Aggregate aggregate = Aggregate.Sum, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetCombineAndStore(operation, destination, keys, weights, aggregate, flags);
    }

    public double SortedSetDecrement(RedisKey key, RedisValue member, double value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetDecrement(key, member, value, flags);
    }

    public double SortedSetIncrement(RedisKey key, RedisValue member, double value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetIncrement(key, member, value, flags);
    }

    public long SortedSetIntersectionLength(RedisKey[] keys, long limit = 0, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetIntersectionLength(keys, limit, flags);
    }

    public long SortedSetLength(RedisKey key, double min = -1, double max = 1, Exclude exclude = Exclude.None,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetLength(key, min, max, exclude, flags);
    }

    public long SortedSetLengthByValue(RedisKey key, RedisValue min, RedisValue max, Exclude exclude = Exclude.None,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetLengthByValue(key, min, max, exclude, flags);
    }

    public RedisValue SortedSetRandomMember(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRandomMember(key, flags);
    }

    public RedisValue[] SortedSetRandomMembers(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRandomMembers(key, count, flags);
    }

    public SortedSetEntry[] SortedSetRandomMembersWithScores(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRandomMembersWithScores(key, count, flags);
    }

    public RedisValue[] SortedSetRangeByRank(RedisKey key, long start = 0, long stop = -1, Order order = Order.Ascending,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRangeByRank(key, start, stop, order, flags);
    }

    public long SortedSetRangeAndStore(RedisKey sourceKey, RedisKey destinationKey, RedisValue start, RedisValue stop,
        SortedSetOrder sortedSetOrder = SortedSetOrder.ByRank, Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0,
        long? take = null, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRangeAndStore(sourceKey, destinationKey, start, stop, sortedSetOrder, exclude, order, skip, take, flags);
    }

    public SortedSetEntry[] SortedSetRangeByRankWithScores(RedisKey key, long start = 0, long stop = -1, Order order = Order.Ascending,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRangeByRankWithScores(key, start, stop, order, flags);
    }

    public RedisValue[] SortedSetRangeByScore(RedisKey key, double start = 0, double stop = 0,
        Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRangeByScore(key, start, stop, exclude, order, skip, take, flags);
    }

    public SortedSetEntry[] SortedSetRangeByScoreWithScores(RedisKey key, double start = -1, double stop = -1,
        Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRangeByScoreWithScores(key, start, stop, exclude, order, skip, take, flags);
    }

    public RedisValue[] SortedSetRangeByValue(RedisKey key, RedisValue min, RedisValue max, Exclude exclude, long skip,
        long take = -1, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRangeByValue(key, min, max, exclude, skip, take, flags);
    }

    public RedisValue[] SortedSetRangeByValue(RedisKey key, RedisValue min = new RedisValue(), RedisValue max = new RedisValue(),
        Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRangeByValue(key, min, max, exclude, order, skip, take, flags);
    }

    public long? SortedSetRank(RedisKey key, RedisValue member, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRank(key, member, order, flags);
    }

    public bool SortedSetRemove(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRemove(key, member, flags);
    }

    public long SortedSetRemove(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRemove(key, members, flags);
    }

    public long SortedSetRemoveRangeByRank(RedisKey key, long start, long stop, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRemoveRangeByRank(key, start, stop, flags);
    }

    public long SortedSetRemoveRangeByScore(RedisKey key, double start, double stop, Exclude exclude = Exclude.None,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRemoveRangeByScore(key, start, stop, exclude, flags);
    }

    public long SortedSetRemoveRangeByValue(RedisKey key, RedisValue min, RedisValue max, Exclude exclude = Exclude.None,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRemoveRangeByValue(key, min, max, exclude, flags);
    }

    public IEnumerable<SortedSetEntry> SortedSetScan(RedisKey key, RedisValue pattern, int pageSize, CommandFlags flags)
    {
        return _databaseImplementation.SortedSetScan(key, pattern, pageSize, flags);
    }

    public IEnumerable<SortedSetEntry> SortedSetScan(RedisKey key, RedisValue pattern = new RedisValue(), int pageSize = 250, long cursor = 0,
        int pageOffset = 0, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetScan(key, pattern, pageSize, cursor, pageOffset, flags);
    }

    public double? SortedSetScore(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetScore(key, member, flags);
    }

    public double?[] SortedSetScores(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetScores(key, members, flags);
    }

    public SortedSetEntry? SortedSetPop(RedisKey key, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetPop(key, order, flags);
    }

    public SortedSetEntry[] SortedSetPop(RedisKey key, long count, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetPop(key, count, order, flags);
    }

    public SortedSetPopResult SortedSetPop(RedisKey[] keys, long count, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetPop(keys, count, order, flags);
    }

    public bool SortedSetUpdate(RedisKey key, RedisValue member, double score, SortedSetWhen when = SortedSetWhen.Always,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetUpdate(key, member, score, when, flags);
    }

    public long SortedSetUpdate(RedisKey key, SortedSetEntry[] values, SortedSetWhen when = SortedSetWhen.Always, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetUpdate(key, values, when, flags);
    }

    public long StreamAcknowledge(RedisKey key, RedisValue groupName, RedisValue messageId, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamAcknowledge(key, groupName, messageId, flags);
    }

    public long StreamAcknowledge(RedisKey key, RedisValue groupName, RedisValue[] messageIds, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamAcknowledge(key, groupName, messageIds, flags);
    }

    public RedisValue StreamAdd(RedisKey key, RedisValue streamField, RedisValue streamValue, RedisValue? messageId = null,
        int? maxLength = null, bool useApproximateMaxLength = false, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamAdd(key, streamField, streamValue, messageId, maxLength, useApproximateMaxLength, flags);
    }

    public RedisValue StreamAdd(RedisKey key, NameValueEntry[] streamPairs, RedisValue? messageId = null, int? maxLength = null,
        bool useApproximateMaxLength = false, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamAdd(key, streamPairs, messageId, maxLength, useApproximateMaxLength, flags);
    }

    public StreamAutoClaimResult StreamAutoClaim(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer,
        long minIdleTimeInMs, RedisValue startAtId, int? count = null, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamAutoClaim(key, consumerGroup, claimingConsumer, minIdleTimeInMs, startAtId, count, flags);
    }

    public StreamAutoClaimIdsOnlyResult StreamAutoClaimIdsOnly(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer,
        long minIdleTimeInMs, RedisValue startAtId, int? count = null, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamAutoClaimIdsOnly(key, consumerGroup, claimingConsumer, minIdleTimeInMs, startAtId, count, flags);
    }

    public StreamEntry[] StreamClaim(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer, long minIdleTimeInMs,
        RedisValue[] messageIds, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamClaim(key, consumerGroup, claimingConsumer, minIdleTimeInMs, messageIds, flags);
    }

    public RedisValue[] StreamClaimIdsOnly(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer,
        long minIdleTimeInMs, RedisValue[] messageIds, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamClaimIdsOnly(key, consumerGroup, claimingConsumer, minIdleTimeInMs, messageIds, flags);
    }

    public bool StreamConsumerGroupSetPosition(RedisKey key, RedisValue groupName, RedisValue position, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamConsumerGroupSetPosition(key, groupName, position, flags);
    }

    public StreamConsumerInfo[] StreamConsumerInfo(RedisKey key, RedisValue groupName, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamConsumerInfo(key, groupName, flags);
    }

    public bool StreamCreateConsumerGroup(RedisKey key, RedisValue groupName, RedisValue? position, CommandFlags flags)
    {
        return _databaseImplementation.StreamCreateConsumerGroup(key, groupName, position, flags);
    }

    public bool StreamCreateConsumerGroup(RedisKey key, RedisValue groupName, RedisValue? position = null,
        bool createStream = true, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamCreateConsumerGroup(key, groupName, position, createStream, flags);
    }

    public long StreamDelete(RedisKey key, RedisValue[] messageIds, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamDelete(key, messageIds, flags);
    }

    public long StreamDeleteConsumer(RedisKey key, RedisValue groupName, RedisValue consumerName, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamDeleteConsumer(key, groupName, consumerName, flags);
    }

    public bool StreamDeleteConsumerGroup(RedisKey key, RedisValue groupName, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamDeleteConsumerGroup(key, groupName, flags);
    }

    public StreamGroupInfo[] StreamGroupInfo(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamGroupInfo(key, flags);
    }

    public StreamInfo StreamInfo(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamInfo(key, flags);
    }

    public long StreamLength(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamLength(key, flags);
    }

    public StreamPendingInfo StreamPending(RedisKey key, RedisValue groupName, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamPending(key, groupName, flags);
    }

    public StreamPendingMessageInfo[] StreamPendingMessages(RedisKey key, RedisValue groupName, int count, RedisValue consumerName,
        RedisValue? minId = null, RedisValue? maxId = null, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamPendingMessages(key, groupName, count, consumerName, minId, maxId, flags);
    }

    public StreamEntry[] StreamRange(RedisKey key, RedisValue? minId = null, RedisValue? maxId = null, int? count = null,
        Order messageOrder = Order.Ascending, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamRange(key, minId, maxId, count, messageOrder, flags);
    }

    public StreamEntry[] StreamRead(RedisKey key, RedisValue position, int? count = null, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamRead(key, position, count, flags);
    }

    public RedisStream[] StreamRead(StreamPosition[] streamPositions, int? countPerStream = null, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamRead(streamPositions, countPerStream, flags);
    }

    public StreamEntry[] StreamReadGroup(RedisKey key, RedisValue groupName, RedisValue consumerName, RedisValue? position,
        int? count, CommandFlags flags)
    {
        return _databaseImplementation.StreamReadGroup(key, groupName, consumerName, position, count, flags);
    }

    public StreamEntry[] StreamReadGroup(RedisKey key, RedisValue groupName, RedisValue consumerName, RedisValue? position = null,
        int? count = null, bool noAck = false, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamReadGroup(key, groupName, consumerName, position, count, noAck, flags);
    }

    public RedisStream[] StreamReadGroup(StreamPosition[] streamPositions, RedisValue groupName, RedisValue consumerName,
        int? countPerStream, CommandFlags flags)
    {
        return _databaseImplementation.StreamReadGroup(streamPositions, groupName, consumerName, countPerStream, flags);
    }

    public RedisStream[] StreamReadGroup(StreamPosition[] streamPositions, RedisValue groupName, RedisValue consumerName,
        int? countPerStream = null, bool noAck = false, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamReadGroup(streamPositions, groupName, consumerName, countPerStream, noAck, flags);
    }

    public long StreamTrim(RedisKey key, int maxLength, bool useApproximateMaxLength = false, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamTrim(key, maxLength, useApproximateMaxLength, flags);
    }

    public long StringAppend(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringAppend(key, value, flags);
    }

    public long StringBitCount(RedisKey key, long start, long end, CommandFlags flags)
    {
        return _databaseImplementation.StringBitCount(key, start, end, flags);
    }

    public long StringBitCount(RedisKey key, long start = 0, long end = -1, StringIndexType indexType = StringIndexType.Byte,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringBitCount(key, start, end, indexType, flags);
    }

    public long StringBitOperation(Bitwise operation, RedisKey destination, RedisKey first, RedisKey second = new RedisKey(),
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringBitOperation(operation, destination, first, second, flags);
    }

    public long StringBitOperation(Bitwise operation, RedisKey destination, RedisKey[] keys, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringBitOperation(operation, destination, keys, flags);
    }

    public long StringBitPosition(RedisKey key, bool bit, long start, long end, CommandFlags flags)
    {
        return _databaseImplementation.StringBitPosition(key, bit, start, end, flags);
    }

    public long StringBitPosition(RedisKey key, bool bit, long start = 0, long end = -1, StringIndexType indexType = StringIndexType.Byte,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringBitPosition(key, bit, start, end, indexType, flags);
    }

    public long StringDecrement(RedisKey key, long value = 1, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringDecrement(key, value, flags);
    }

    public bool IsConnected(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.IsConnected(key, flags);
    }

    public Task KeyMigrateAsync(RedisKey key, EndPoint toServer, int toDatabase = 0, int timeoutMilliseconds = 0,
        MigrateOptions migrateOptions = MigrateOptions.None, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyMigrateAsync(key, toServer, toDatabase, timeoutMilliseconds, migrateOptions, flags);
    }

    public Task<RedisValue> DebugObjectAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.DebugObjectAsync(key, flags);
    }

    public Task<bool> GeoAddAsync(RedisKey key, double longitude, double latitude, RedisValue member, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoAddAsync(key, longitude, latitude, member, flags);
    }

    public Task<bool> GeoAddAsync(RedisKey key, GeoEntry value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoAddAsync(key, value, flags);
    }

    public Task<long> GeoAddAsync(RedisKey key, GeoEntry[] values, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoAddAsync(key, values, flags);
    }

    public Task<bool> GeoRemoveAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoRemoveAsync(key, member, flags);
    }

    public Task<double?> GeoDistanceAsync(RedisKey key, RedisValue member1, RedisValue member2, GeoUnit unit = GeoUnit.Meters,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoDistanceAsync(key, member1, member2, unit, flags);
    }

    public Task<string?[]> GeoHashAsync(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoHashAsync(key, members, flags);
    }

    public Task<string?> GeoHashAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoHashAsync(key, member, flags);
    }

    public Task<GeoPosition?[]> GeoPositionAsync(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoPositionAsync(key, members, flags);
    }

    public Task<GeoPosition?> GeoPositionAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoPositionAsync(key, member, flags);
    }

    public Task<GeoRadiusResult[]> GeoRadiusAsync(RedisKey key, RedisValue member, double radius, GeoUnit unit = GeoUnit.Meters, int count = -1,
        Order? order = null, GeoRadiusOptions options = GeoRadiusOptions.Default, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoRadiusAsync(key, member, radius, unit, count, order, options, flags);
    }

    public Task<GeoRadiusResult[]> GeoRadiusAsync(RedisKey key, double longitude, double latitude, double radius, GeoUnit unit = GeoUnit.Meters,
        int count = -1, Order? order = null, GeoRadiusOptions options = GeoRadiusOptions.Default, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoRadiusAsync(key, longitude, latitude, radius, unit, count, order, options, flags);
    }

    public Task<GeoRadiusResult[]> GeoSearchAsync(RedisKey key, RedisValue member, GeoSearchShape shape, int count = -1, bool demandClosest = true,
        Order? order = null, GeoRadiusOptions options = GeoRadiusOptions.Default, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoSearchAsync(key, member, shape, count, demandClosest, order, options, flags);
    }

    public Task<GeoRadiusResult[]> GeoSearchAsync(RedisKey key, double longitude, double latitude, GeoSearchShape shape, int count = -1,
        bool demandClosest = true, Order? order = null, GeoRadiusOptions options = GeoRadiusOptions.Default, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoSearchAsync(key, longitude, latitude, shape, count, demandClosest, order, options, flags);
    }

    public Task<long> GeoSearchAndStoreAsync(RedisKey sourceKey, RedisKey destinationKey, RedisValue member, GeoSearchShape shape,
        int count = -1, bool demandClosest = true, Order? order = null, bool storeDistances = false,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoSearchAndStoreAsync(sourceKey, destinationKey, member, shape, count, demandClosest, order, storeDistances, flags);
    }

    public Task<long> GeoSearchAndStoreAsync(RedisKey sourceKey, RedisKey destinationKey, double longitude, double latitude,
        GeoSearchShape shape, int count = -1, bool demandClosest = true, Order? order = null, bool storeDistances = false,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.GeoSearchAndStoreAsync(sourceKey, destinationKey, longitude, latitude, shape, count, demandClosest, order, storeDistances, flags);
    }

    public Task<long> HashDecrementAsync(RedisKey key, RedisValue hashField, long value = 1, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashDecrementAsync(key, hashField, value, flags);
    }

    public Task<double> HashDecrementAsync(RedisKey key, RedisValue hashField, double value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashDecrementAsync(key, hashField, value, flags);
    }

    public Task<bool> HashDeleteAsync(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashDeleteAsync(key, hashField, flags);
    }

    public Task<long> HashDeleteAsync(RedisKey key, RedisValue[] hashFields, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashDeleteAsync(key, hashFields, flags);
    }

    public Task<bool> HashExistsAsync(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashExistsAsync(key, hashField, flags);
    }

    public Task<RedisValue> HashGetAsync(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashGetAsync(key, hashField, flags);
    }

    public Task<Lease<byte>?> HashGetLeaseAsync(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashGetLeaseAsync(key, hashField, flags);
    }

    public Task<RedisValue[]> HashGetAsync(RedisKey key, RedisValue[] hashFields, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashGetAsync(key, hashFields, flags);
    }

    public Task<HashEntry[]> HashGetAllAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashGetAllAsync(key, flags);
    }

    public Task<long> HashIncrementAsync(RedisKey key, RedisValue hashField, long value = 1, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashIncrementAsync(key, hashField, value, flags);
    }

    public Task<double> HashIncrementAsync(RedisKey key, RedisValue hashField, double value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashIncrementAsync(key, hashField, value, flags);
    }

    public Task<RedisValue[]> HashKeysAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashKeysAsync(key, flags);
    }

    public Task<long> HashLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashLengthAsync(key, flags);
    }

    public Task<RedisValue> HashRandomFieldAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashRandomFieldAsync(key, flags);
    }

    public Task<RedisValue[]> HashRandomFieldsAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashRandomFieldsAsync(key, count, flags);
    }

    public Task<HashEntry[]> HashRandomFieldsWithValuesAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashRandomFieldsWithValuesAsync(key, count, flags);
    }

    public IAsyncEnumerable<HashEntry> HashScanAsync(RedisKey key, RedisValue pattern = new RedisValue(), int pageSize = 250, long cursor = 0,
        int pageOffset = 0, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashScanAsync(key, pattern, pageSize, cursor, pageOffset, flags);
    }

    public Task HashSetAsync(RedisKey key, HashEntry[] hashFields, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashSetAsync(key, hashFields, flags);
    }

    public Task<bool> HashSetAsync(RedisKey key, RedisValue hashField, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashSetAsync(key, hashField, value, when, flags);
    }

    public Task<long> HashStringLengthAsync(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashStringLengthAsync(key, hashField, flags);
    }

    public Task<RedisValue[]> HashValuesAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HashValuesAsync(key, flags);
    }

    public Task<bool> HyperLogLogAddAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HyperLogLogAddAsync(key, value, flags);
    }

    public Task<bool> HyperLogLogAddAsync(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HyperLogLogAddAsync(key, values, flags);
    }

    public Task<long> HyperLogLogLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HyperLogLogLengthAsync(key, flags);
    }

    public Task<long> HyperLogLogLengthAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HyperLogLogLengthAsync(keys, flags);
    }

    public Task HyperLogLogMergeAsync(RedisKey destination, RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HyperLogLogMergeAsync(destination, first, second, flags);
    }

    public Task HyperLogLogMergeAsync(RedisKey destination, RedisKey[] sourceKeys, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.HyperLogLogMergeAsync(destination, sourceKeys, flags);
    }

    public Task<EndPoint?> IdentifyEndpointAsync(RedisKey key = new RedisKey(), CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.IdentifyEndpointAsync(key, flags);
    }

    public Task<bool> KeyCopyAsync(RedisKey sourceKey, RedisKey destinationKey, int destinationDatabase = -1, bool replace = false,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyCopyAsync(sourceKey, destinationKey, destinationDatabase, replace, flags);
    }

    public Task<bool> KeyDeleteAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyDeleteAsync(key, flags);
    }

    public Task<long> KeyDeleteAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyDeleteAsync(keys, flags);
    }

    public Task<byte[]?> KeyDumpAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyDumpAsync(key, flags);
    }

    public Task<string?> KeyEncodingAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyEncodingAsync(key, flags);
    }

    public Task<bool> KeyExistsAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return Task.FromResult(KeyExists(key, flags));
    }

    public Task<long> KeyExistsAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyExistsAsync(keys, flags);
    }

    public Task<bool> KeyExpireAsync(RedisKey key, TimeSpan? expiry, CommandFlags flags)
    {
        return _databaseImplementation.KeyExpireAsync(key, expiry, flags);
    }

    public Task<bool> KeyExpireAsync(RedisKey key, TimeSpan? expiry, ExpireWhen when = ExpireWhen.Always, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyExpireAsync(key, expiry, when, flags);
    }

    public Task<bool> KeyExpireAsync(RedisKey key, DateTime? expiry, CommandFlags flags)
    {
        return _databaseImplementation.KeyExpireAsync(key, expiry, flags);
    }

    public Task<bool> KeyExpireAsync(RedisKey key, DateTime? expiry, ExpireWhen when = ExpireWhen.Always, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyExpireAsync(key, expiry, when, flags);
    }

    public Task<DateTime?> KeyExpireTimeAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyExpireTimeAsync(key, flags);
    }

    public Task<long?> KeyFrequencyAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyFrequencyAsync(key, flags);
    }

    public Task<TimeSpan?> KeyIdleTimeAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyIdleTimeAsync(key, flags);
    }

    public Task<bool> KeyMoveAsync(RedisKey key, int database, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyMoveAsync(key, database, flags);
    }

    public Task<bool> KeyPersistAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyPersistAsync(key, flags);
    }

    public Task<RedisKey> KeyRandomAsync(CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyRandomAsync(flags);
    }

    public Task<long?> KeyRefCountAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyRefCountAsync(key, flags);
    }

    public Task<bool> KeyRenameAsync(RedisKey key, RedisKey newKey, When when = When.Always, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyRenameAsync(key, newKey, when, flags);
    }

    public Task KeyRestoreAsync(RedisKey key, byte[] value, TimeSpan? expiry = null, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyRestoreAsync(key, value, expiry, flags);
    }

    public Task<TimeSpan?> KeyTimeToLiveAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyTimeToLiveAsync(key, flags);
    }

    public Task<bool> KeyTouchAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyTouchAsync(key, flags);
    }

    public Task<long> KeyTouchAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyTouchAsync(keys, flags);
    }

    public Task<RedisType> KeyTypeAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.KeyTypeAsync(key, flags);
    }

    public Task<RedisValue> ListGetByIndexAsync(RedisKey key, long index, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListGetByIndexAsync(key, index, flags);
    }

    public Task<long> ListInsertAfterAsync(RedisKey key, RedisValue pivot, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListInsertAfterAsync(key, pivot, value, flags);
    }

    public Task<long> ListInsertBeforeAsync(RedisKey key, RedisValue pivot, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListInsertBeforeAsync(key, pivot, value, flags);
    }

    public Task<RedisValue> ListLeftPopAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListLeftPopAsync(key, flags);
    }

    public Task<RedisValue[]> ListLeftPopAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListLeftPopAsync(key, count, flags);
    }

    public Task<ListPopResult> ListLeftPopAsync(RedisKey[] keys, long count, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListLeftPopAsync(keys, count, flags);
    }

    public Task<long> ListPositionAsync(RedisKey key, RedisValue element, long rank = 1, long maxLength = 0, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListPositionAsync(key, element, rank, maxLength, flags);
    }

    public Task<long[]> ListPositionsAsync(RedisKey key, RedisValue element, long count, long rank = 1, long maxLength = 0,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListPositionsAsync(key, element, count, rank, maxLength, flags);
    }

    public Task<long> ListLeftPushAsync(RedisKey key, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListLeftPushAsync(key, value, when, flags);
    }

    public Task<long> ListLeftPushAsync(RedisKey key, RedisValue[] values, When when = When.Always, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListLeftPushAsync(key, values, when, flags);
    }

    public Task<long> ListLeftPushAsync(RedisKey key, RedisValue[] values, CommandFlags flags)
    {
        return _databaseImplementation.ListLeftPushAsync(key, values, flags);
    }

    public Task<long> ListLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListLengthAsync(key, flags);
    }

    public Task<RedisValue> ListMoveAsync(RedisKey sourceKey, RedisKey destinationKey, ListSide sourceSide, ListSide destinationSide,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListMoveAsync(sourceKey, destinationKey, sourceSide, destinationSide, flags);
    }

    public Task<RedisValue[]> ListRangeAsync(RedisKey key, long start = 0, long stop = -1, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListRangeAsync(key, start, stop, flags);
    }

    public Task<long> ListRemoveAsync(RedisKey key, RedisValue value, long count = 0, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListRemoveAsync(key, value, count, flags);
    }

    public Task<RedisValue> ListRightPopAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListRightPopAsync(key, flags);
    }

    public Task<RedisValue[]> ListRightPopAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListRightPopAsync(key, count, flags);
    }

    public Task<ListPopResult> ListRightPopAsync(RedisKey[] keys, long count, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListRightPopAsync(keys, count, flags);
    }

    public Task<RedisValue> ListRightPopLeftPushAsync(RedisKey source, RedisKey destination, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListRightPopLeftPushAsync(source, destination, flags);
    }

    public Task<long> ListRightPushAsync(RedisKey key, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListRightPushAsync(key, value, when, flags);
    }

    public Task<long> ListRightPushAsync(RedisKey key, RedisValue[] values, When when = When.Always, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListRightPushAsync(key, values, when, flags);
    }

    public Task<long> ListRightPushAsync(RedisKey key, RedisValue[] values, CommandFlags flags)
    {
        return _databaseImplementation.ListRightPushAsync(key, values, flags);
    }

    public Task ListSetByIndexAsync(RedisKey key, long index, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListSetByIndexAsync(key, index, value, flags);
    }

    public Task ListTrimAsync(RedisKey key, long start, long stop, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ListTrimAsync(key, start, stop, flags);
    }

    public Task<bool> LockExtendAsync(RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.LockExtendAsync(key, value, expiry, flags);
    }

    public Task<RedisValue> LockQueryAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.LockQueryAsync(key, flags);
    }

    public Task<bool> LockReleaseAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.LockReleaseAsync(key, value, flags);
    }

    public Task<bool> LockTakeAsync(RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.LockTakeAsync(key, value, expiry, flags);
    }

    public Task<long> PublishAsync(RedisChannel channel, RedisValue message, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.PublishAsync(channel, message, flags);
    }

    public Task<RedisResult> ExecuteAsync(string command, params object[] args)
    {
        return _databaseImplementation.ExecuteAsync(command, args);
    }

    public Task<RedisResult> ExecuteAsync(string command, ICollection<object>? args, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ExecuteAsync(command, args, flags);
    }

    public Task<RedisResult> ScriptEvaluateAsync(string script, RedisKey[]? keys = null, RedisValue[]? values = null,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ScriptEvaluateAsync(script, keys, values, flags);
    }

    public Task<RedisResult> ScriptEvaluateAsync(byte[] hash, RedisKey[]? keys = null, RedisValue[]? values = null, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ScriptEvaluateAsync(hash, keys, values, flags);
    }

    public Task<RedisResult> ScriptEvaluateAsync(LuaScript script, object? parameters = null, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ScriptEvaluateAsync(script, parameters, flags);
    }

    public Task<RedisResult> ScriptEvaluateAsync(LoadedLuaScript script, object? parameters = null, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ScriptEvaluateAsync(script, parameters, flags);
    }

    public Task<RedisResult> ScriptEvaluateReadOnlyAsync(string script, RedisKey[]? keys = null, RedisValue[]? values = null,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ScriptEvaluateReadOnlyAsync(script, keys, values, flags);
    }

    public Task<RedisResult> ScriptEvaluateReadOnlyAsync(byte[] hash, RedisKey[]? keys = null, RedisValue[]? values = null,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.ScriptEvaluateReadOnlyAsync(hash, keys, values, flags);
    }

    public Task<bool> SetAddAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetAddAsync(key, value, flags);
    }

    public Task<long> SetAddAsync(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetAddAsync(key, values, flags);
    }

    public Task<RedisValue[]> SetCombineAsync(SetOperation operation, RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetCombineAsync(operation, first, second, flags);
    }

    public Task<RedisValue[]> SetCombineAsync(SetOperation operation, RedisKey[] keys, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetCombineAsync(operation, keys, flags);
    }

    public Task<long> SetCombineAndStoreAsync(SetOperation operation, RedisKey destination, RedisKey first, RedisKey second,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetCombineAndStoreAsync(operation, destination, first, second, flags);
    }

    public Task<long> SetCombineAndStoreAsync(SetOperation operation, RedisKey destination, RedisKey[] keys, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetCombineAndStoreAsync(operation, destination, keys, flags);
    }

    public Task<bool> SetContainsAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetContainsAsync(key, value, flags);
    }

    public Task<bool[]> SetContainsAsync(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetContainsAsync(key, values, flags);
    }

    public Task<long> SetIntersectionLengthAsync(RedisKey[] keys, long limit = 0, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetIntersectionLengthAsync(keys, limit, flags);
    }

    public Task<long> SetLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetLengthAsync(key, flags);
    }

    public Task<RedisValue[]> SetMembersAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetMembersAsync(key, flags);
    }

    public Task<bool> SetMoveAsync(RedisKey source, RedisKey destination, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetMoveAsync(source, destination, value, flags);
    }

    public Task<RedisValue> SetPopAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetPopAsync(key, flags);
    }

    public Task<RedisValue[]> SetPopAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetPopAsync(key, count, flags);
    }

    public Task<RedisValue> SetRandomMemberAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetRandomMemberAsync(key, flags);
    }

    public Task<RedisValue[]> SetRandomMembersAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetRandomMembersAsync(key, count, flags);
    }

    public Task<bool> SetRemoveAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetRemoveAsync(key, value, flags);
    }

    public Task<long> SetRemoveAsync(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetRemoveAsync(key, values, flags);
    }

    public IAsyncEnumerable<RedisValue> SetScanAsync(RedisKey key, RedisValue pattern = new RedisValue(), int pageSize = 250, long cursor = 0,
        int pageOffset = 0, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SetScanAsync(key, pattern, pageSize, cursor, pageOffset, flags);
    }

    public Task<RedisValue[]> SortAsync(RedisKey key, long skip = 0, long take = -1, Order order = Order.Ascending, SortType sortType = SortType.Numeric,
        RedisValue by = new RedisValue(), RedisValue[]? get = null, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortAsync(key, skip, take, order, sortType, by, get, flags);
    }

    public Task<long> SortAndStoreAsync(RedisKey destination, RedisKey key, long skip = 0, long take = -1, Order order = Order.Ascending,
        SortType sortType = SortType.Numeric, RedisValue by = new RedisValue(), RedisValue[]? get = null, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortAndStoreAsync(destination, key, skip, take, order, sortType, by, get, flags);
    }

    public Task<bool> SortedSetAddAsync(RedisKey key, RedisValue member, double score, CommandFlags flags)
    {
        return _databaseImplementation.SortedSetAddAsync(key, member, score, flags);
    }

    public Task<bool> SortedSetAddAsync(RedisKey key, RedisValue member, double score, When when, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetAddAsync(key, member, score, when, flags);
    }

    public Task<bool> SortedSetAddAsync(RedisKey key, RedisValue member, double score, SortedSetWhen when = SortedSetWhen.Always,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetAddAsync(key, member, score, when, flags);
    }

    public Task<long> SortedSetAddAsync(RedisKey key, SortedSetEntry[] values, CommandFlags flags)
    {
        return _databaseImplementation.SortedSetAddAsync(key, values, flags);
    }

    public Task<long> SortedSetAddAsync(RedisKey key, SortedSetEntry[] values, When when, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetAddAsync(key, values, when, flags);
    }

    public Task<long> SortedSetAddAsync(RedisKey key, SortedSetEntry[] values, SortedSetWhen when = SortedSetWhen.Always, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetAddAsync(key, values, when, flags);
    }

    public Task<RedisValue[]> SortedSetCombineAsync(SetOperation operation, RedisKey[] keys, double[]? weights = null, Aggregate aggregate = Aggregate.Sum,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetCombineAsync(operation, keys, weights, aggregate, flags);
    }

    public Task<SortedSetEntry[]> SortedSetCombineWithScoresAsync(SetOperation operation, RedisKey[] keys, double[]? weights = null,
        Aggregate aggregate = Aggregate.Sum, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetCombineWithScoresAsync(operation, keys, weights, aggregate, flags);
    }

    public Task<long> SortedSetCombineAndStoreAsync(SetOperation operation, RedisKey destination, RedisKey first, RedisKey second,
        Aggregate aggregate = Aggregate.Sum, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetCombineAndStoreAsync(operation, destination, first, second, aggregate, flags);
    }

    public Task<long> SortedSetCombineAndStoreAsync(SetOperation operation, RedisKey destination, RedisKey[] keys,
        double[]? weights = null, Aggregate aggregate = Aggregate.Sum, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetCombineAndStoreAsync(operation, destination, keys, weights, aggregate, flags);
    }

    public Task<double> SortedSetDecrementAsync(RedisKey key, RedisValue member, double value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetDecrementAsync(key, member, value, flags);
    }

    public Task<double> SortedSetIncrementAsync(RedisKey key, RedisValue member, double value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetIncrementAsync(key, member, value, flags);
    }

    public Task<long> SortedSetIntersectionLengthAsync(RedisKey[] keys, long limit = 0, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetIntersectionLengthAsync(keys, limit, flags);
    }

    public Task<long> SortedSetLengthAsync(RedisKey key, double min = -1, double max = -1, Exclude exclude = Exclude.None,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetLengthAsync(key, min, max, exclude, flags);
    }

    public Task<long> SortedSetLengthByValueAsync(RedisKey key, RedisValue min, RedisValue max, Exclude exclude = Exclude.None,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetLengthByValueAsync(key, min, max, exclude, flags);
    }

    public Task<RedisValue> SortedSetRandomMemberAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRandomMemberAsync(key, flags);
    }

    public Task<RedisValue[]> SortedSetRandomMembersAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRandomMembersAsync(key, count, flags);
    }

    public Task<SortedSetEntry[]> SortedSetRandomMembersWithScoresAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRandomMembersWithScoresAsync(key, count, flags);
    }

    public Task<RedisValue[]> SortedSetRangeByRankAsync(RedisKey key, long start = 0, long stop = -1, Order order = Order.Ascending,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRangeByRankAsync(key, start, stop, order, flags);
    }

    public Task<long> SortedSetRangeAndStoreAsync(RedisKey sourceKey, RedisKey destinationKey, RedisValue start, RedisValue stop,
        SortedSetOrder sortedSetOrder = SortedSetOrder.ByRank, Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0,
        long? take = null, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRangeAndStoreAsync(sourceKey, destinationKey, start, stop, sortedSetOrder, exclude, order, skip, take, flags);
    }

    public Task<SortedSetEntry[]> SortedSetRangeByRankWithScoresAsync(RedisKey key, long start = 0, long stop = -1, Order order = Order.Ascending,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRangeByRankWithScoresAsync(key, start, stop, order, flags);
    }

    public Task<RedisValue[]> SortedSetRangeByScoreAsync(RedisKey key, double start = -1, double stop = 1, Exclude exclude = Exclude.None,
        Order order = Order.Ascending, long skip = 0, long take = -1, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRangeByScoreAsync(key, start, stop, exclude, order, skip, take, flags);
    }

    public Task<SortedSetEntry[]> SortedSetRangeByScoreWithScoresAsync(RedisKey key, double start = -1, double stop = 1,
        Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRangeByScoreWithScoresAsync(key, start, stop, exclude, order, skip, take, flags);
    }

    public Task<RedisValue[]> SortedSetRangeByValueAsync(RedisKey key, RedisValue min, RedisValue max, Exclude exclude, long skip,
        long take = -1, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRangeByValueAsync(key, min, max, exclude, skip, take, flags);
    }

    public Task<RedisValue[]> SortedSetRangeByValueAsync(RedisKey key, RedisValue min = new RedisValue(), RedisValue max = new RedisValue(),
        Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRangeByValueAsync(key, min, max, exclude, order, skip, take, flags);
    }

    public Task<long?> SortedSetRankAsync(RedisKey key, RedisValue member, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRankAsync(key, member, order, flags);
    }

    public Task<bool> SortedSetRemoveAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRemoveAsync(key, member, flags);
    }

    public Task<long> SortedSetRemoveAsync(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRemoveAsync(key, members, flags);
    }

    public Task<long> SortedSetRemoveRangeByRankAsync(RedisKey key, long start, long stop, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRemoveRangeByRankAsync(key, start, stop, flags);
    }

    public Task<long> SortedSetRemoveRangeByScoreAsync(RedisKey key, double start, double stop, Exclude exclude = Exclude.None,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRemoveRangeByScoreAsync(key, start, stop, exclude, flags);
    }

    public Task<long> SortedSetRemoveRangeByValueAsync(RedisKey key, RedisValue min, RedisValue max, Exclude exclude = Exclude.None,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetRemoveRangeByValueAsync(key, min, max, exclude, flags);
    }

    public IAsyncEnumerable<SortedSetEntry> SortedSetScanAsync(RedisKey key, RedisValue pattern = new RedisValue(), int pageSize = 250,
        long cursor = 0, int pageOffset = 0, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetScanAsync(key, pattern, pageSize, cursor, pageOffset, flags);
    }

    public Task<double?> SortedSetScoreAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetScoreAsync(key, member, flags);
    }

    public Task<double?[]> SortedSetScoresAsync(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetScoresAsync(key, members, flags);
    }

    public Task<bool> SortedSetUpdateAsync(RedisKey key, RedisValue member, double score, SortedSetWhen when = SortedSetWhen.Always,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetUpdateAsync(key, member, score, when, flags);
    }

    public Task<long> SortedSetUpdateAsync(RedisKey key, SortedSetEntry[] values, SortedSetWhen when = SortedSetWhen.Always,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetUpdateAsync(key, values, when, flags);
    }

    public Task<SortedSetEntry?> SortedSetPopAsync(RedisKey key, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetPopAsync(key, order, flags);
    }

    public Task<SortedSetEntry[]> SortedSetPopAsync(RedisKey key, long count, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetPopAsync(key, count, order, flags);
    }

    public Task<SortedSetPopResult> SortedSetPopAsync(RedisKey[] keys, long count, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.SortedSetPopAsync(keys, count, order, flags);
    }

    public Task<long> StreamAcknowledgeAsync(RedisKey key, RedisValue groupName, RedisValue messageId, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamAcknowledgeAsync(key, groupName, messageId, flags);
    }

    public Task<long> StreamAcknowledgeAsync(RedisKey key, RedisValue groupName, RedisValue[] messageIds, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamAcknowledgeAsync(key, groupName, messageIds, flags);
    }

    public Task<RedisValue> StreamAddAsync(RedisKey key, RedisValue streamField, RedisValue streamValue, RedisValue? messageId = null,
        int? maxLength = null, bool useApproximateMaxLength = false, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamAddAsync(key, streamField, streamValue, messageId, maxLength, useApproximateMaxLength, flags);
    }

    public Task<RedisValue> StreamAddAsync(RedisKey key, NameValueEntry[] streamPairs, RedisValue? messageId = null, int? maxLength = null,
        bool useApproximateMaxLength = false, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamAddAsync(key, streamPairs, messageId, maxLength, useApproximateMaxLength, flags);
    }

    public Task<StreamAutoClaimResult> StreamAutoClaimAsync(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer, long minIdleTimeInMs,
        RedisValue startAtId, int? count = null, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamAutoClaimAsync(key, consumerGroup, claimingConsumer, minIdleTimeInMs, startAtId, count, flags);
    }

    public Task<StreamAutoClaimIdsOnlyResult> StreamAutoClaimIdsOnlyAsync(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer,
        long minIdleTimeInMs, RedisValue startAtId, int? count = null, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamAutoClaimIdsOnlyAsync(key, consumerGroup, claimingConsumer, minIdleTimeInMs, startAtId, count, flags);
    }

    public Task<StreamEntry[]> StreamClaimAsync(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer, long minIdleTimeInMs,
        RedisValue[] messageIds, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamClaimAsync(key, consumerGroup, claimingConsumer, minIdleTimeInMs, messageIds, flags);
    }

    public Task<RedisValue[]> StreamClaimIdsOnlyAsync(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer, long minIdleTimeInMs,
        RedisValue[] messageIds, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamClaimIdsOnlyAsync(key, consumerGroup, claimingConsumer, minIdleTimeInMs, messageIds, flags);
    }

    public Task<bool> StreamConsumerGroupSetPositionAsync(RedisKey key, RedisValue groupName, RedisValue position,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamConsumerGroupSetPositionAsync(key, groupName, position, flags);
    }

    public Task<StreamConsumerInfo[]> StreamConsumerInfoAsync(RedisKey key, RedisValue groupName, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamConsumerInfoAsync(key, groupName, flags);
    }

    public Task<bool> StreamCreateConsumerGroupAsync(RedisKey key, RedisValue groupName, RedisValue? position, CommandFlags flags)
    {
        return _databaseImplementation.StreamCreateConsumerGroupAsync(key, groupName, position, flags);
    }

    public Task<bool> StreamCreateConsumerGroupAsync(RedisKey key, RedisValue groupName, RedisValue? position = null,
        bool createStream = true, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamCreateConsumerGroupAsync(key, groupName, position, createStream, flags);
    }

    public Task<long> StreamDeleteAsync(RedisKey key, RedisValue[] messageIds, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamDeleteAsync(key, messageIds, flags);
    }

    public Task<long> StreamDeleteConsumerAsync(RedisKey key, RedisValue groupName, RedisValue consumerName, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamDeleteConsumerAsync(key, groupName, consumerName, flags);
    }

    public Task<bool> StreamDeleteConsumerGroupAsync(RedisKey key, RedisValue groupName, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamDeleteConsumerGroupAsync(key, groupName, flags);
    }

    public Task<StreamGroupInfo[]> StreamGroupInfoAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamGroupInfoAsync(key, flags);
    }

    public Task<StreamInfo> StreamInfoAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamInfoAsync(key, flags);
    }

    public Task<long> StreamLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamLengthAsync(key, flags);
    }

    public Task<StreamPendingInfo> StreamPendingAsync(RedisKey key, RedisValue groupName, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamPendingAsync(key, groupName, flags);
    }

    public Task<StreamPendingMessageInfo[]> StreamPendingMessagesAsync(RedisKey key, RedisValue groupName, int count, RedisValue consumerName,
        RedisValue? minId = null, RedisValue? maxId = null, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamPendingMessagesAsync(key, groupName, count, consumerName, minId, maxId, flags);
    }

    public Task<StreamEntry[]> StreamRangeAsync(RedisKey key, RedisValue? minId = null, RedisValue? maxId = null, int? count = null,
        Order messageOrder = Order.Ascending, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamRangeAsync(key, minId, maxId, count, messageOrder, flags);
    }

    public Task<StreamEntry[]> StreamReadAsync(RedisKey key, RedisValue position, int? count = null, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamReadAsync(key, position, count, flags);
    }

    public Task<RedisStream[]> StreamReadAsync(StreamPosition[] streamPositions, int? countPerStream = null, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamReadAsync(streamPositions, countPerStream, flags);
    }

    public Task<StreamEntry[]> StreamReadGroupAsync(RedisKey key, RedisValue groupName, RedisValue consumerName, RedisValue? position, int? count,
        CommandFlags flags)
    {
        return _databaseImplementation.StreamReadGroupAsync(key, groupName, consumerName, position, count, flags);
    }

    public Task<StreamEntry[]> StreamReadGroupAsync(RedisKey key, RedisValue groupName, RedisValue consumerName, RedisValue? position = null,
        int? count = null, bool noAck = false, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamReadGroupAsync(key, groupName, consumerName, position, count, noAck, flags);
    }

    public Task<RedisStream[]> StreamReadGroupAsync(StreamPosition[] streamPositions, RedisValue groupName, RedisValue consumerName,
        int? countPerStream, CommandFlags flags)
    {
        return _databaseImplementation.StreamReadGroupAsync(streamPositions, groupName, consumerName, countPerStream, flags);
    }

    public Task<RedisStream[]> StreamReadGroupAsync(StreamPosition[] streamPositions, RedisValue groupName, RedisValue consumerName,
        int? countPerStream = null, bool noAck = false, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamReadGroupAsync(streamPositions, groupName, consumerName, countPerStream, noAck, flags);
    }

    public Task<long> StreamTrimAsync(RedisKey key, int maxLength, bool useApproximateMaxLength = false, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StreamTrimAsync(key, maxLength, useApproximateMaxLength, flags);
    }

    public Task<long> StringAppendAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringAppendAsync(key, value, flags);
    }

    public Task<long> StringBitCountAsync(RedisKey key, long start, long end, CommandFlags flags)
    {
        return _databaseImplementation.StringBitCountAsync(key, start, end, flags);
    }

    public Task<long> StringBitCountAsync(RedisKey key, long start = 0, long end = -1, StringIndexType indexType = StringIndexType.Byte,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringBitCountAsync(key, start, end, indexType, flags);
    }

    public Task<long> StringBitOperationAsync(Bitwise operation, RedisKey destination, RedisKey first, RedisKey second = new RedisKey(),
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringBitOperationAsync(operation, destination, first, second, flags);
    }

    public Task<long> StringBitOperationAsync(Bitwise operation, RedisKey destination, RedisKey[] keys, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringBitOperationAsync(operation, destination, keys, flags);
    }

    public Task<long> StringBitPositionAsync(RedisKey key, bool bit, long start, long end, CommandFlags flags)
    {
        return _databaseImplementation.StringBitPositionAsync(key, bit, start, end, flags);
    }

    public Task<long> StringBitPositionAsync(RedisKey key, bool bit, long start = 0, long end = -1, StringIndexType indexType = StringIndexType.Byte,
        CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringBitPositionAsync(key, bit, start, end, indexType, flags);
    }

    public Task<long> StringDecrementAsync(RedisKey key, long value = 1, CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.StringDecrementAsync(key, value, flags);
    }

    public Task<TimeSpan> PingAsync(CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.PingAsync(flags);
    }

    public bool TryWait(Task task)
    {
        return _databaseImplementation.TryWait(task);
    }

    public void Wait(Task task)
    {
        _databaseImplementation.Wait(task);
    }

    public T Wait<T>(Task<T> task)
    {
        return _databaseImplementation.Wait(task);
    }

    public void WaitAll(params Task[] tasks)
    {
        _databaseImplementation.WaitAll(tasks);
    }

    public IConnectionMultiplexer Multiplexer => _databaseImplementation.Multiplexer;

    public TimeSpan Ping(CommandFlags flags = CommandFlags.None)
    {
        return _databaseImplementation.Ping(flags);
    }
}