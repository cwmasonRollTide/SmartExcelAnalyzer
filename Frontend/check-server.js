import { get } from 'http';

get('http://localhost:3000', (res) => {
  let data = '';

  res.on('data', (chunk) => {
    data += chunk;
  });

  res.on('end', () => {
    console.log(data);
  });
}).on('error', (err) => {
  console.log("Error: " + err.message);
});