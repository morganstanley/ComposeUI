import React from 'react';

export default function Footer() {
  const date = new Date();
  return (
    <footer className="footer-main">
      <div className="content">
        <ul>
          <li>
            <a
              href="https://github.com/MorganStanley/"
              target="_blank"
              rel="noreferrer"
            >
              Github
            </a>
          </li>
          <li>
            <a
              href="https://github.com/MorganStanley/"
              target="_blank"
              rel="noreferrer"
            >
              Morgan Stanley
            </a>
          </li>
          <hr />
        </ul>
        <p>&copy;{date.getFullYear()} Morgan Stanley. All rights reserved.</p>
      </div>
    </footer>
  );
}
