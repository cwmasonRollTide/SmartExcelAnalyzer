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

const QueryResult: React.FC<QueryResultProps> = ({ result }: QueryResultProps) => {
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
        Question: {question}
      </Typography>
      <Typography 
        variant="body1"
      >
        Answer: {answer}
      </Typography>
      <Typography 
        variant="body2"
      >
        Document ID: {documentId}
      </Typography>

      {relevantRows && relevantRows.length > 0 ? (
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow>
                {Object.keys(relevantRows[0]).map((key) => (
                  <TableCell key={key}>{key}</TableCell>
                ))}
              </TableRow>
            </TableHead>
            <TableBody>
              {relevantRows.map((row, index) => (
                <TableRow key={index}>
                  {Object.values(row).map((value, idx) => (
                    <TableCell key={idx}>{value}</TableCell>
                  ))}
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      ) : (
        <Typography variant="body2">No relevant rows found.</Typography>
      )}
    </Box>
  );
};

export default QueryResult;