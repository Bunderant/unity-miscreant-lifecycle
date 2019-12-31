namespace Miscreant.Lifecycle
{
	internal sealed class IntrusiveFixedUpdateList : IntrusiveList
	{
		protected override void ExecuteCurrent()
		{
			current.ManagedFixedUpdate();
		}

		protected override void Advance()
		{
			current = current.nextFixedUpdate;
		}

		internal override void AddToTail(CustomUpdateBehaviour component)
		{
			if (!ReferenceEquals(component.nextFixedUpdate, null) || !ReferenceEquals(component.previousFixedUpdate, null))
			{
				return;
			}
			count++;

			if (ReferenceEquals(head, null))
			{
				head = component;
				head.previousFixedUpdate = head;
				head.nextFixedUpdate = head;
				return;
			}

			Add(component, head.previousFixedUpdate, head);
		}

		private void Add(CustomUpdateBehaviour node, CustomUpdateBehaviour prev, CustomUpdateBehaviour next)
		{
			node.nextFixedUpdate = next;
			node.previousFixedUpdate = prev;

			next.previousFixedUpdate = node;
			prev.nextFixedUpdate = node;
		}

		internal override void Remove(CustomUpdateBehaviour component)
		{
			// Only need to check one of the link references. Since the lists are circular, if one of these refs is null, both must be null. 
			if (ReferenceEquals(component.nextFixedUpdate, null))
			{
				return;
			}
			count--;

			if (count == 0)
			{
				// TODO: MIscreant: Elegantly handle "current" field  when removing the last element in the list. Need to work out semantics there, too. 
				head.previousFixedUpdate = null;
				head.nextFixedUpdate = null;
				head = null;
				return;
			}

			if (ReferenceEquals(component, head))
			{
				head = head.nextFixedUpdate;
			}

			component.nextFixedUpdate.previousFixedUpdate = component.previousFixedUpdate;
			component.previousFixedUpdate.nextFixedUpdate = component.nextFixedUpdate;

			// This specifically handles the case where a component destroys itself from any type of managed update callback. By resetting 'current' to the 
			// previous link, 'current' will not be null when 'Advance()' is called, and we'll process the expected next link during the next iteration. 
			// Since it was the 'current' update callback that brought us here, there's no danger of accidentally processing 'current' a second time.
			if (ReferenceEquals(current, component))
			{
				current = component.previousFixedUpdate;
			}

			component.previousFixedUpdate = null;
			component.nextFixedUpdate = null;
		}
	}
}