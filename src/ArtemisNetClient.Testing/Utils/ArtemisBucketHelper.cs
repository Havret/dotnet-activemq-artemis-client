namespace ActiveMQ.Artemis.Client.Testing.Utils;

internal static class ArtemisBucketHelper
{
    public static int GetBucket(string groupId, int bucketCount)
    {
        var bytes = ToBytes(groupId);
        var hashCode = ToHashCode(bytes);
        return (hashCode & int.MaxValue) % bucketCount;
    }

    private static byte[] ToBytes(string groupId)
    {
        var data = new byte[groupId.Length << 1];

        int j = 0;

        foreach (var c in groupId)
        {
            byte low = (byte) (c & 0xFF); // low byte

            data[j++] = low;

            byte high = (byte) (c >> 8 & 0xFF); // high byte

            data[j++] = high;                
        }

        return data;
    }

    private static int ToHashCode(byte[] bytes)
    {
        var hashCode = 0;
        foreach (var element in bytes)
        {
            hashCode = (hashCode << 5) - hashCode + element; // (hash << 5) - hash is same as hash * 31
        }
        return hashCode;
    }
}