using System;
using static Assertions;

public class Pool<T> {
    public delegate T CreatePooledItem();
    public T[]              Items;
    public CreatePooledItem Factory;
    public int              ItemsCount;
    
    public Pool(CreatePooledItem factory) {
        Factory    = factory;
        Items      = new T[30];
        ItemsCount = 0;
    }
    
    public Pool(CreatePooledItem factory, int initialCapacity) {
        Factory    = factory;
        Items      = new T[initialCapacity];
        ItemsCount = 0;
    }
    
    public T Get() {
        if(ItemsCount > 0) {
            return Items[--ItemsCount];
        }else {
            Assert(Factory != null);
            return Factory();
        }
        
    }
    
    public void Return(T item) {
        if(ItemsCount >= Items.Length) {
            Array.Resize(ref Items, ItemsCount << 1);
        }
        
        Items[ItemsCount++] = item;
    }
}