using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Miscreant.Utilities.Lifecycle
{
	internal sealed class IntrusiveUpdateList : IntrusiveList
	{
		public IntrusiveUpdateList(CustomUpdatePriority priorityGroup) : base(priorityGroup)
		{ }

		protected override void ExecuteCurrent()
		{
			current.ManagedUpdate();
		}

		protected override void Advance()
		{
			current = current.nextUpdate;
		}

		internal override void AddToTail(CustomUpdateBehaviour component)
		{
			if (!ReferenceEquals(component.nextUpdate, null) || !ReferenceEquals(component.previousUpdate, null))
			{
				return;
			}
			count++;

			if (ReferenceEquals(head, null))
			{
				head = component;
				head.previousUpdate = head;
				head.nextUpdate = head;
				return;
			}

			Add(component, head.previousUpdate, head);
		}

		private void Add(CustomUpdateBehaviour node, CustomUpdateBehaviour prev, CustomUpdateBehaviour next)
		{
			node.nextUpdate = next;
			node.previousUpdate = prev;

			next.previousUpdate = node;
			prev.nextUpdate = node;
		}

		internal override void Remove(CustomUpdateBehaviour component)
		{
			if (ReferenceEquals(component.nextUpdate, null) && ReferenceEquals(component.previousUpdate, null))
			{
				return;
			}
			count--;

			if (count == 0)
			{
				// TODO: Miscreant: Restore this under a "strict" conditional
				// if (!ReferenceEquals(component, head))
				// {
				// 	throw new ArgumentException("component is not the root of the list");
				// }

				// TODO: MIscreant: Elegantly handle "current" field  when removing the last element in the list. Need to work out semantics there, too. 

				head.previousUpdate = null;
				head.nextUpdate = null;
				head = null;
				return;
			}

			if (ReferenceEquals(component, head))
			{
				head = head.nextUpdate;
			}

			component.nextUpdate.previousUpdate = component.previousUpdate;
			component.previousUpdate.nextUpdate = component.nextUpdate;

			if (ReferenceEquals(current, component))
			{
				current = component.previousUpdate;
			}

			component.previousUpdate = null;
			component.nextUpdate = null;
		}
	}
}
