using System;

namespace NT
{
    public sealed class Hierarchy<T> {
        Hierarchy<T> parent;
        Hierarchy<T> sibling;
        Hierarchy<T> child;
        public T owner;

        public Hierarchy() {
            parent = null;
            sibling = null;
            child = null;
            owner = default(T);
        }

        public Hierarchy(T _owner) {
            parent = null;
            sibling = null;
            child = null;
            owner = _owner;
        }

        public T GetParent() {
            return parent != null ? parent.owner : default(T);
        }

        public T GetChild() {
            return child != null ? child.owner : default(T);
        }

        public Hierarchy<T> GetChildNode() {
            return child;
        }

        public T GetSibling() {
            return sibling != null ? sibling.owner : default(T);
        }

        public Hierarchy<T> GetParentNode() {
            return parent;
        }

        public Hierarchy<T> GetPreviousSiblingNode() {
            if (parent == null || parent.child == this) {
                return null;
            }

            Hierarchy<T> prev = null;
            Hierarchy<T> node = parent.child;
            while (node != this && node != null) {
                prev = node;
                node = node.sibling;
            }

            if (node != this) {
                throw new InvalidOperationException("Hierarchy::GetPreviousSibling: Could not find node in parent's list of children.");
            }

            return prev;
        }

        public T GetPreviousSibling() {
            var prev = GetPreviousSiblingNode();
            if (prev != null) {
                return prev.owner;
            }

            return default(T);
        }

        public Hierarchy<T> GetNextSiblingNode() {
            return sibling;
        }

        public Hierarchy<T> GetNextNode() {
            if (child != null) {
                return child;
            } else {
                var node = this;
                while (node != null && node.sibling == null) {
                    node = node.parent;
                }

                if (node != null) {
                    return node.sibling;
                } else {
                    return null;
                }
            }
        }

        public T GetNext() {
            if (child != null) {
                return child.owner;
            } else {
                var node = this;
                while (node != null && node.sibling == null) {
                    node = node.parent;
                }

                if (node != null) {
                    return node.sibling.owner;
                } else {
                    return default(T);
                }
            }
        }

        public T GetNextLeaf() {
            Hierarchy<T> node = null;

            if (child != null) {
                node = child;
                while (node.child != null) {
                    node = node.child;
                }
                return node.owner;
            } else {
                node = this;
                while (node != null && node.sibling == null) {
                    node = node.parent;
                }
                if (node != null) {
                    node = node.sibling;
                    while (node.child != null) {
                        node = node.child;
                    }
                    return node.owner;
                } else {
                    return default(T);
                }
            }
        }

        public void ParentTo(Hierarchy<T> node) {
            RemoveFromParent();

            parent = node;
            sibling = node.child;
            node.child = this;
        }

        public void MakeSiblingAfter(Hierarchy<T> node) {
            RemoveFromParent();

            parent = node.parent;
            sibling = node.sibling;
            node.sibling = this;
        }

        public bool ParentedBy(Hierarchy<T> node) {
            if (parent == node) {
                return true;
            } else if (parent != null) {
                return parent.ParentedBy(node);
            } else {
                return false;
            }
        }

        public void RemoveFromParent() {
            Hierarchy<T> prev = null;

            if (parent != null) {
                prev = GetPreviousSiblingNode();
                if (prev != null) {
                    prev.sibling = sibling;
                } else {
                    parent.child = sibling;
                }
            }

            parent = null;
            sibling = null;
        }

        public void RemoveFromHierarchy() {
            Hierarchy<T> parentNode = parent;
            Hierarchy<T> node;

            RemoveFromParent();

            if (parentNode != null) {
                while (child != null) {
                    node = child;
                    node.RemoveFromParent();
                    node.ParentTo(parentNode);
                }
            } else {
                while (child != null) {
                    child.RemoveFromParent();
                }
            }
        }
    }
}