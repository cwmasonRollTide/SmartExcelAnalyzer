import React from 'react';
import { 
  List, 
  ListItem, 
  ListItemButton, 
  ListItemText 
} from '@mui/material';

interface Document {
  id: string;
  name: string;
}

interface DocumentListProps {
  documents: Document[];
  selectedDocument: Document | null;
  onSelectDocument: (document: Document) => void;
}

const DocumentList: React.FC<DocumentListProps> = ({
  documents,
  selectedDocument,
  onSelectDocument,
}) => {
  return (
    <List 
      sx={{ 
        width: '100%', 
        maxWidth: 360, 
        bgcolor: 'background.paper' 
      }}
    >
      {documents.map((document) => (
        <ListItem
          disablePadding
          key={document.id} 
        >
          <ListItemButton
            selected={selectedDocument?.id === document.id}
            onClick={() => onSelectDocument(document)}
          >
            <ListItemText primary={document.name} />
          </ListItemButton>
        </ListItem>
      ))}
    </List>
  );
};

export default DocumentList;