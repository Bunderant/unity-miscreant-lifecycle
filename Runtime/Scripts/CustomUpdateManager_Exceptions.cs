using System;

namespace Miscreant.Utilities.Lifecycle
{
	using UpdateType = CustomUpdateManager.UpdateType;

	public abstract class CustomUpdateManagerException : Exception
	{
		public CustomUpdateManagerException(string message) { }
	}

	//
	// General
	//

	public class InvalidUpdateTypeException : CustomUpdateManagerException
	{
		public InvalidUpdateTypeException(UpdateType type) : base($"{nameof(UpdateType)} cannot be specified as {type}.") { }
	}

	//
	// List State
	//

	public class DuplicateReferenceException : CustomUpdateManagerException
	{
		public DuplicateReferenceException() : base("Linked list contains more than one reference of the same component.") { }
	}

	public class EmptyGroupWithNonNullHeadOrTailException : CustomUpdateManagerException
	{
		public EmptyGroupWithNonNullHeadOrTailException() : base("For an empty group, both head and tail must be null.") { }
	}

	public class NonemptyGroupWithNullHeadOrTailException : CustomUpdateManagerException
	{
		public NonemptyGroupWithNullHeadOrTailException() : base("For a non-empty group, neither head nor tail can be null.") { }
	}

	public class SingleElementGroupWithDifferentHeadAndTailException : CustomUpdateManagerException
	{
		public SingleElementGroupWithDifferentHeadAndTailException() : base("For a single-item group, head and tail must be the same component.") { }
	}

	public class InvalidTailLinkException : CustomUpdateManagerException
	{
		public InvalidTailLinkException() : base("Tail component's link must be set to null.") { }
	}

	public class TailMismatchException : CustomUpdateManagerException
	{
		public TailMismatchException() : base("Final link in the group does not match the expected tail.") { }
	}

	public class InvalidCountException : CustomUpdateManagerException
	{
		public InvalidCountException() : base("Total number of components in group does not match the expected count.") { }
	}

	//
	// Component State
	//

	public class PriorityGroupMismatchException : CustomUpdateManagerException
	{
		public PriorityGroupMismatchException() : base("Element's priority group does not match the expected group.") { }
	}

	public class ManagerMismatchException : CustomUpdateManagerException
	{
		public ManagerMismatchException() : base("Assigned manager does not match the current system.") { }
	}
}
