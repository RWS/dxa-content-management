Tridion.Type.registerNamespace("DXA.CM.Extensions.CustomResolver.Models");

$models.$dcr = DXA.CM.Extensions.CustomResolver.Models.Factory = new (function ()
{
	// Mandatory
	this.getItem = function Factory$getItem(id)
	{
		var item = $models.getFromRepository(id);
		if (!item)
		{
			item = $models.createInRepository(id, this.getItemType(id), id);
		}
		if (Tridion.OO.implementsInterface(item, "Tridion.IdentifiableObject") && item.getId())
		{
			//this will initialize the item with static data, if found in any of the loaded lists
			$models.updateItemData(item);
		}
		return item;
	};

	// Mandatory
	this.getItemType = function Factory$getItemType(item)
	{
		if (item.match(/^dcr\:configuration$/))
		{
			return Tridion.Constants.ItemType.DCR_CONFIGURATION;
		}
		else
		{
			item = $models.getFromRepository(item); // if the item is in the repository then ask it for its item type
		}

		if (item && Tridion.OO.implementsInterface(item, "Tridion.IdentifiableObject"))
		{
			return item.getItemType();
		}
	};

	this.addItem = function Factory$addItem(id, item)
	{
		if (!id)
		{
			id = "sg:" + $models.getUniqueId();
		}
		$models.addToRepository(id, item);
		return id;
	};
})();
