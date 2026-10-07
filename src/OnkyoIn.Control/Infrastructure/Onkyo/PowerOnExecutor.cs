namespace OnkyoIn.Control.Infrastructure.Onkyo;

/// <summary>
/// Encapsulates the "power on, then tolerate a late acknowledgement" policy.
///
/// Onkyo receivers coming out of standby can take longer than the library's
/// default timeout to emit their <c>PWR01</c> acknowledgement. In that case the
/// command has usually already been applied, so instead of failing we verify the
/// real power state before surfacing an error.
/// </summary>
public static class PowerOnExecutor
{
    public static async Task ExecuteAsync(Func<Task> sendPowerOn, Func<Task<bool>> isPoweredOn)
    {
        try
        {
            await sendPowerOn();
        }
        catch (TimeoutException)
        {
            if (!await isPoweredOn())
            {
                throw;
            }
        }
    }
}
