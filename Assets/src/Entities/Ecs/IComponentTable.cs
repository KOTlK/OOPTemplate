using System;

public interface IComponentTable {
	uint GetComponentId();
	uint GetComponentsCount();
	Type GetComponentType();

	void Remove(uint entity);
}