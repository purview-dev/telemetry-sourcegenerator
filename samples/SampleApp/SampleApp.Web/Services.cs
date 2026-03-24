namespace SampleApp
{
	internal class Services(ILogger<Services> logger)
	{
		public void THING()
		{
			logger.LogInformation("HELLO: {Thing}", "WORLD");
		}
	}
}
