namespace MinApiLib.OidcServer.Services;

/// <summary>
/// User code generator using 9 digit number
/// </summary>
/// <seealso cref="MinApiLib.OidcServer.Services.IUserCodeGenerator" />
[RegisterInIServiceCollection(typeof(IUserCodeGenerator))]
public class NumericUserCodeGenerator : IUserCodeGenerator
{
    /// <summary>
    /// Gets the type of the user code.
    /// </summary>
    /// <value>
    /// The type of the user code.
    /// </value>
    public string UserCodeType => OidcServerConstants.UserCodeTypes.Numeric;

    /// <summary>
    /// Gets the retry limit.
    /// </summary>
    /// <value>
    /// The retry limit for getting a unique value.
    /// </value>
    public int RetryLimit => 5;

    /// <summary>
    /// Generates the user code.
    /// </summary>
    /// <returns></returns>
    public Task<string> GenerateAsync()
    {
        var next = Next(100000000, 999999999);
        return Task.FromResult(next.ToString(CultureInfo.InvariantCulture));
    }

    private int Next(int minValue, int maxValue)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minValue, maxValue);
        if (minValue == maxValue) return minValue;
        long diff = maxValue - minValue;

        var uint32Buffer = new byte[8];

        using (var rng = RandomNumberGenerator.Create())
        {
            while (true)
            {
                rng.GetBytes(uint32Buffer);
                var rand = BitConverter.ToUInt32(uint32Buffer, 0);

                const long max = 1 + (long)uint.MaxValue;
                var remainder = max % diff;
                if (rand < max - remainder)
                {
                    return (int)(minValue + rand % diff);
                }
            }
        }
    }
}
