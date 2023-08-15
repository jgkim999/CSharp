using Bogus;

namespace WebApiDomain.Utils
{
    public static class SessionIdGenerator
    {
        private static readonly Faker faker = new Faker();
        private static readonly HashidsNet.Hashids hashids = new HashidsNet.Hashids("WebApiDomainSessionIdGenerator");

        public static string GetId(long id)
        {
            var randomLong = faker.Random.Long(0, int.MaxValue);
            return hashids.EncodeLong(id, randomLong);
        }

        public static long GetId(string id)
        {
            var decode = hashids.DecodeLong(id);
            return decode[0];
        }
    }
}
