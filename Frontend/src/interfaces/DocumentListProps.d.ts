import { Document } from "./Document.tsx";
export interface DocumentListProps {
    documents: Document[];
    selectedDocument: Document | null;
    onSelectDocument: (document: Document) => void;
}
