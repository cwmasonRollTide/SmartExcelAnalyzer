import React, { Component } from 'react';
import { QueryResponse } from '../../services/api';
import { Typography, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper } from '@mui/material';

interface QueryResultProps {
  result: QueryResponse | null;
}

const QueryResult: React.FC<QueryResultProps> = ({ result }) => {
  if (!result) {
    return <Component paragraph>No data available</Component>;
  }

  return (
    <div>
      <Typography variant="h6" gutterBottom>
        Answer:
      </Typography>
      <Component paragraph>{result.answer}</Component>
      <Typography variant="h6" gutterBottom>
        Relevant Data:
      </Typography>
      <TableContainer component={Paper}>
        <Table sx={{ minWidth: 650 }} aria-label="simple table">
          <TableHead>
            <TableRow>
              {Object.keys(result.relevantRows[0] || {}).map((key) => (
                <TableCell key={key}>{key}</TableCell>
              ))}
            </TableRow>
          </TableHead>
          <TableBody>
            {result.relevantRows.map((row, index) => (
              <TableRow key={index}>
                {Object.values(row).map((value: any, cellIndex) => (
                  <TableCell key={cellIndex}>{value}</TableCell>
                ))}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </div>
  );
};

export default QueryResult;