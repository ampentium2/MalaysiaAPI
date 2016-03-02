package md57095c8c500f0cc62b0d872c5192ae981;


public class CoreCodeModel
	extends android.app.Activity
	implements
		mono.android.IGCUserPeer
{
	static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("MalaysiaAPI.CoreCodeModel, MalaysiaAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", CoreCodeModel.class, __md_methods);
	}


	public CoreCodeModel () throws java.lang.Throwable
	{
		super ();
		if (getClass () == CoreCodeModel.class)
			mono.android.TypeManager.Activate ("MalaysiaAPI.CoreCodeModel, MalaysiaAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "", this, new java.lang.Object[] {  });
	}

	java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
