import React from 'react';
import { 
  Table, 
  TableBody, 
  TableCell, 
  TableContainer, 
  TableHead, 
  TableRow, 
  Paper, 
  Typography, 
  Box 
} from '@mui/material';
import { QueryResultProps } from '../../interfaces/QueryResultProps';

const QueryResult: React.FC<QueryResultProps> = ({ result }) => {
  const { answer, question, documentId, relevantRows } = result;

  return (
    <Box sx={{ mt: 2 }}>
      <Typography 
        variant="h6" 
        gutterBottom
      >
        Query Result
      </Typography>
      <Typography 
        variant="body1"
      >
        <strong>
          Question:
        </strong> 
        {question}
      </Typography>
      <Typography 
        variant="body1"
      >
        <strong>
          Answer:
        </strong> 
        {answer}
      </Typography>
      <Typography 
        variant="body2"
      >
        <strong>
          Document ID:
        </strong> 
        {documentId}
      </Typography>

      {relevantRows && relevantRows.length > 0 ? (
        <Box sx={{ mt: 2 }}>
          <Typography 
            variant="h6" 
            gutterBottom
          >
            Relevant Data
          </Typography>
          <Paper 
            elevation={3} 
            sx={{ 
              backgroundColor: '#f0f0f0', 
              boxShadow: 'inset 0 2px 4px 0 rgba(0,0,0,0.1)',
              borderRadius: 2,
              overflow: 'hidden'
            }}
          >
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    {Object.keys(relevantRows[0]).map((header) => (
                      <TableCell 
                        key={header} 
                        sx={{ fontWeight: 'bold' }}
                      >
                        {header}
                      </TableCell>
                    ))}
                  </TableRow>
                </TableHead>
                <TableBody>
                  {relevantRows.map((row, index) => (
                    <TableRow key={index}>
                      {Object.entries(row).map(([key, value]) => (
                        <TableCell 
                          key={`${index}-${key}`}
                        >
                          {value?.toString() || ''}
                        </TableCell>
                      ))}
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </Paper>
        </Box>
      ) : (
        <Typography 
          variant="body2" 
          sx={{ mt: 2 }}
        >
          No relevant data found.
        </Typography>
      )}
    </Box>
  );
};

export default QueryResult;
