using System;

public interface IComponentTable {
	uint GetComponentId();
	int  GetComponentsCount();
	Type GetComponentType();

	void Remove(EntityHandle handle);
}