import { createTheme } from '@mui/material/styles';

export const theme = createTheme({
  palette: {
    primary: {
      main: '#187aba',
    },
    secondary: {
      main: '#002b51',
    },
  },
  components: {
    MuiMenuItem: {
      styleOverrides: {
        root: {
          '&.Mui-selected': { backgroundColor: 'primary.main' },
        },
      },
    },
  },
});
