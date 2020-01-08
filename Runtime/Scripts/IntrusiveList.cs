using System;

namespace Miscreant.Lifecycle
{
	internal abstract class IntrusiveList
	{
		public ManagedUpdatesBehaviour head = null;
		public uint count = 0;

		protected ManagedUpdatesBehaviour current;

		public void ExecuteAll()
		{
			if (count == 0)
			{
				return;
			}

			current = head;
			do
			{
				ExecuteCurrent();
				Advance();
			} while (!ReferenceEquals(current, head));
			current = null;
		}

		public void Traverse(Action<ManagedUpdatesBehaviour> perElementAction)
		{
			if (count == 0)
			{
				return;
			}

			current = head;
			do
			{
				perElementAction.Invoke(current);
				Advance();
			} while (!ReferenceEquals(current, head));
			current = null;
		}

		protected abstract void ExecuteCurrent();
		protected abstract void Advance();

		internal abstract void AddToTail(ManagedUpdatesBehaviour component);
		internal abstract void Remove(ManagedUpdatesBehaviour component);
	}
}