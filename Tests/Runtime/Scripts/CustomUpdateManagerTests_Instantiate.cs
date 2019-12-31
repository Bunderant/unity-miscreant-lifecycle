using NUnit.Framework;
using System;

namespace Miscreant.Lifecycle.RuntimeTests
{
	using ObjectToggleConfig = FakeEnvironment.ObjectToggleConfig;

	public abstract class CustomUpdateManagerTests_Instantiate
	{
		protected FakeEnvironment environment;

		[SetUp]
		public void SetUp()
		{
			SetUpEnvironment();
		}

		[TearDown]
		public void TearDown()
		{
			TearDownEnvironment();
		}

		protected abstract void SetUpEnvironment();
		private void TearDownEnvironment()
		{
			environment.Dispose();
			environment = null;
		}

		[Test]
		[TestCaseSource(typeof(CustomUpdateManagerTests), nameof(CustomUpdateManagerTests.allActiveTogglePermutations))]
		public void Instantiate_SingleObjectInstantiated_AddedToSystem(ObjectToggleConfig config)
		{
			// Arrange
			FakeEnvironment.GetExpectedUpdateFlagsFromConfig(
				config,
				out bool updateExpected,
				out bool fixedUpdateExpected
			);

			if (!(updateExpected || fixedUpdateExpected))
			{
				throw new ArgumentException(
					"Invalid input for test. Config must be expected to have at least one type of update callback registered:\n" + config
				);
			}

			// Act
			environment.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
				out TestBasicManagedUpdatesComponent[] components,
				CustomUpdateManagerTests.DEFAULT_GROUP_NAME,
				config
			);

			// Assert
			environment.manager.CheckSystemForComponent(components[0], out bool updateFound, out bool fixedUpdateFound);
			Assert.That(
				(updateExpected == updateFound) && (fixedUpdateExpected == fixedUpdateFound),
				$"Instantiated component should have at least one update callback registered with the manager, matching what was expected.\n" +
					$"Update Expected/Result: {updateExpected}/{updateFound}\n" +
					$"FixedUpdate Expected/Result: {fixedUpdateExpected}/{fixedUpdateFound}"
			);
		}

		[Test]
		[TestCaseSource(typeof(CustomUpdateManagerTests), nameof(CustomUpdateManagerTests.allInactiveTogglePermutations))]
		public void Instantiate_SingleObjectInstantiated_NotAddedToSystem(ObjectToggleConfig config)
		{
			// Arrange
			FakeEnvironment.GetExpectedUpdateFlagsFromConfig(
				config,
				out bool updateExpected,
				out bool fixedUpdateExpected
			);

			if (updateExpected || fixedUpdateExpected)
			{
				throw new ArgumentException(
					"Invalid input for test. Config must specify NONE of the update callbacks to be registered:\n" + config
				);
			}

			// Act
			environment.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
				out TestBasicManagedUpdatesComponent[] components,
				CustomUpdateManagerTests.DEFAULT_GROUP_NAME,
				config
			);

			// Assert
			environment.manager.CheckSystemForComponent(components[0], out bool updateFound, out bool fixedUpdateFound);
			Assert.That(
				(updateExpected == updateFound) && (fixedUpdateExpected == fixedUpdateFound),
				$"Instantiated component should not have any update callbacks registered with the manager.\n" +
					$"Update Expected/Result: {updateExpected}/{updateFound}\n" +
					$"FixedUpdate Expected/Result: {fixedUpdateExpected}/{fixedUpdateFound}"
			);
		}
	}
}