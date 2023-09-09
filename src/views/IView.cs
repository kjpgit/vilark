// Copyright (C) 2023 Karl Pickett / ViLark Project
namespace vilark;

abstract class IView
{
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
    public virtual void Draw(Console console) { }

    // (re)position and enable your cursor, if it is active.
    // This is done after all windows are done drawing, to avoid flicker.
    public virtual void UpdateCursor(Console console) { }

    // your window dimensions changed, so update sizes of all of your children.
    public virtual void OnResize() { }

    public virtual void OnKeyPress(KeyPress kp) { }

    // Fields for all instances
    protected DrawRect m_size;
    protected bool m_is_visible = false;
}
