namespace Miscreant.Lifecycle.RuntimeTests
{
	public sealed class CustomUpdateManagerTests_InstantiateFromEmptySystem : CustomUpdateManagerTests_Instantiate
	{
		protected override void SetUpEnvironment()
		{
			environment = new FakeEnvironment(CustomUpdateManagerTests.DEFAULT_GROUP_NAME);
		}
	}
}