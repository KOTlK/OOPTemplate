using System;

public class Pool<T> {
    public Func<T>          OnCreate;
    public Action<T>        OnRelease;
    public T[]              Items;
    public int              ItemsCount;
    
    public Pool(Func<T> onCreate, Action<T> onRelease) {
        OnCreate   = onCreate;
        OnRelease  = onRelease;
        Items      = new T[32];
        ItemsCount = 0;
    }
    
    public Pool(Func<T> onCreate, Action<T> onRelease, int initialCapacity) {
        OnCreate   = onCreate;
        OnRelease  = onRelease;
        Items      = new T[initialCapacity];
        ItemsCount = 0;
    }
    
    public T Get() {
        if(ItemsCount > 0) {
            return Items[--ItemsCount];
        }else {
            return OnCreate();
        }
    }
    
    public void Release(T item) {
        if(ItemsCount >= Items.Length) {
            Array.Resize(ref Items, ItemsCount << 1);
        }
        
        Items[ItemsCount++] = item;
        OnRelease(item);
    }
}