namespace Miscreant.Lifecycle.RuntimeTests
{
	public sealed class ManagedExecutionSystemTests_InstantiateFromEmptySystem : ManagedExecutionSystemTests_Instantiate
	{
		protected override void SetUpEnvironment()
		{
			environment = new FakeEnvironment(TestData.DEFAULT_GROUP_NAME);
		}
	}
}