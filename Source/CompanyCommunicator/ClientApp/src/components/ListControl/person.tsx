import React from 'react';
import { Persona, PersonaSize, PersonaPresence } from 'office-ui-fabric-react/lib/Persona';
import './person.scss';

interface IProps {
  url: string;
  name: string;
}

/**
 * Creat Person Componenet
 * 
 */

class Person extends React.Component<IProps>{

  componentDidMount() {
    this.updateWindowDimensions();
    window.addEventListener('resize', this.updateWindowDimensions);
  }

  componentWillUnmount() {
    window.removeEventListener('resize', this.updateWindowDimensions);
  }

  updateWindowDimensions() {
    let imgs = document.getElementsByClassName('ms-Persona-coin');
    if ((window.outerWidth - 16) <= 1366) {
      for (let i = 0; i < imgs.length; i++) {
        imgs[i].classList.add("invisible");
      }
    } else {
      for (let i = 0; i < imgs.length; i++) {
        imgs[i].classList.remove("invisible");
      }
    }
  }

  render() {
    return (
      <Persona
        imageUrl={this.props.url}
        text={this.props.name}
        size={PersonaSize.size32}
        presence={PersonaPresence.away}
        hidePersonaDetails={false}
      />
    );
  }
}

export default Person;
