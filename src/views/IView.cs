// Copyright (C) 2023 Karl Pickett / Vilark Project
namespace vilark;

abstract class IView
{
    public IView(IView? parent) {
        if (parent != null) {
            parent.AddChild(this);
        }
    }

    public void DrawIfVisible(Console console) {
        if (IsVisible && Size.height > 0) {
            Draw(console);
        }
    }

    public void Resize(DrawRect newSize) {
        if (newSize != m_size) {
            m_size = newSize;
            OnResize();
        }
    }

    public void SetVisible(bool visible) { m_is_visible = visible; }
    public bool IsVisible => m_is_visible;
    public DrawRect Size => m_size;

    // (re)draw your entire frame to the console
    // By default, it draws all your visible children.
    public virtual void Draw(Console console) {
        DrawChildrenIfVisible(console);
    }

    // (re)position and enable your cursor, if it is active.
    // This is done after all windows are done drawing, to avoid flicker.
    public virtual void UpdateCursor(Console console) { }

    // your window dimensions changed, so update sizes of all of your children.
    public virtual void OnResize() { }

    public virtual void OnKeyPress(KeyPress kp) { }

    protected void DrawChildrenIfVisible(Console console) {
        if (m_children != null) {
            Log.Info($"Drawing children for {this} ({m_children.Count})");
            foreach (var child in m_children) {
                Log.Info($"Child is {child}, {child.IsVisible}");
                child.DrawIfVisible(console);
            }
        }
    }

    private void AddChild(IView child) {
        if (m_children == null) {
            m_children = new();
        }
        m_children.Add(child);
    }

    // Fields for all instances
    private DrawRect m_size;
    private bool m_is_visible = false;
    private List<IView>? m_children = null;
}
