import React, { Component } from 'react';
import { NavMenu } from './NavMenu';
import { Outlet } from 'react-router';

export class Layout extends Component {
  static displayName = Layout.name;

  render () {
    return (
      <div>
        <NavMenu />
        <Outlet />
      </div>
    );
  }
}
