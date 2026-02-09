using Reflex.Core;

// LOL. Incapsulation in it's peak form.
public class ReflexRootContainer {
	public static Container Container { get; private set; }

	public ReflexRootContainer(Container container) {
		Container = container;
	}
}