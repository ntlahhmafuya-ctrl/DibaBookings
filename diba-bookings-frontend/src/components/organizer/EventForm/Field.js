function Field({ label, required, children }) { return <label className="field"><span>{label}{required && " *"}</span>{children}</label>; }
