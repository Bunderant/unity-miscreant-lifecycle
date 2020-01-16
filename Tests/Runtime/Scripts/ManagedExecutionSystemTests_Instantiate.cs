using NUnit.Framework;
using System;

namespace Miscreant.Lifecycle.RuntimeTests
{
	using ObjectToggleConfig = FakeEnvironment.ObjectToggleConfig;

	public sealed class ManagedExecutionSystemTests_Instantiate
	{
		private FakeEnvironment _environment;

		[SetUp]
		public void SetUp()
		{
			_environment = new FakeEnvironment(TestData.DEFAULT_GROUP_NAME);
		}

		[TearDown]
		public void TearDown()
		{
			_environment.Dispose();
			_environment = null;
		}

		private void PopulateEnvironment()
		{
			_environment.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
				TestData.DEFAULT_GROUP_NAME,
				TestData.allTogglePermutations
			);
		}

		[Test]
		[TestCaseSource(typeof(TestData), nameof(TestData.allActiveTogglePermutations))]
		public void Instantiate_SingleObjectInstantiatedFromEmpty_AddedToSystem(ObjectToggleConfig config)
		{
			// Validatate test case input
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
			_environment.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
				out TestBasicManagedUpdatesComponent[] components,
				TestData.DEFAULT_GROUP_NAME,
				config
			);

			// Assert
			_environment.system.FindComponent(components[0].GetInstanceID(), out bool updateFound, out bool fixedUpdateFound);
			Assert.That(
				(updateExpected == updateFound) && (fixedUpdateExpected == fixedUpdateFound),
				$"Instantiated component should have at least one update callback registered with the system, matching what was expected.\n" +
					$"Update Expected/Result: {updateExpected}/{updateFound}\n" +
					$"FixedUpdate Expected/Result: {fixedUpdateExpected}/{fixedUpdateFound}"
			);
		}

		[Test]
		[TestCaseSource(typeof(TestData), nameof(TestData.allInactiveTogglePermutations))]
		public void Instantiate_SingleObjectInstantiatedFromEmpty_NotAddedToSystem(ObjectToggleConfig config)
		{
			// Validate test case input
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
			_environment.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
				out TestBasicManagedUpdatesComponent[] components,
				TestData.DEFAULT_GROUP_NAME,
				config
			);

			// Assert
			_environment.system.FindComponent(components[0].GetInstanceID(), out bool updateFound, out bool fixedUpdateFound);
			Assert.That(
				(updateExpected == updateFound) && (fixedUpdateExpected == fixedUpdateFound),
				$"Instantiated component should not have any update callbacks registered with the system.\n" +
					$"Update Expected/Result: {updateExpected}/{updateFound}\n" +
					$"FixedUpdate Expected/Result: {fixedUpdateExpected}/{fixedUpdateFound}"
			);
		}

		[Test]
		[TestCaseSource(typeof(TestData), nameof(TestData.allActiveTogglePermutations))]
		public void Instantiate_SingleObjectInstantiatedFromPopulated_AddedToSystem(ObjectToggleConfig config)
		{
			// Validatate test case input
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

			// Arrange
			PopulateEnvironment();

			// Act
			_environment.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
				out TestBasicManagedUpdatesComponent[] components,
				TestData.DEFAULT_GROUP_NAME,
				config
			);

			// Assert
			_environment.system.FindComponent(components[0].GetInstanceID(), out bool updateFound, out bool fixedUpdateFound);
			Assert.That(
				(updateExpected == updateFound) && (fixedUpdateExpected == fixedUpdateFound),
				$"Instantiated component should have at least one update callback registered with the system, matching what was expected.\n" +
					$"Update Expected/Result: {updateExpected}/{updateFound}\n" +
					$"FixedUpdate Expected/Result: {fixedUpdateExpected}/{fixedUpdateFound}"
			);
		}

		[Test]
		[TestCaseSource(typeof(TestData), nameof(TestData.allInactiveTogglePermutations))]
		public void Instantiate_SingleObjectInstantiatedFromPopulated_NotAddedToSystem(ObjectToggleConfig config)
		{
			// Validatate test case input
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

			// Arrange
			PopulateEnvironment();

			// Act
			_environment.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
				out TestBasicManagedUpdatesComponent[] components,
				TestData.DEFAULT_GROUP_NAME,
				config
			);

			// Assert
			_environment.system.FindComponent(components[0].GetInstanceID(), out bool updateFound, out bool fixedUpdateFound);
			Assert.That(
				(updateExpected == updateFound) && (fixedUpdateExpected == fixedUpdateFound),
				$"Instantiated component should not have any update callbacks registered with the system.\n" +
					$"Update Expected/Result: {updateExpected}/{updateFound}\n" +
					$"FixedUpdate Expected/Result: {fixedUpdateExpected}/{fixedUpdateFound}"
			);
		}
	}
}