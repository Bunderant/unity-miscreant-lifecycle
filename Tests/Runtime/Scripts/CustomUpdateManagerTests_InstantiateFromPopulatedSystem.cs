namespace Miscreant.Lifecycle.RuntimeTests
{
	public sealed class CustomUpdateManagerTests_InstantiateFromPopulatedSystem : CustomUpdateManagerTests_Instantiate
	{
		protected override void SetUpEnvironment()
		{
			environment = new FakeEnvironment(TestData.DEFAULT_GROUP_NAME);

			environment.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
				TestData.DEFAULT_GROUP_NAME,
				TestData.allTogglePermutations
			);
		}
	}
}