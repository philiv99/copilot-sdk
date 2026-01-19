/**
 * Tests for the EnvironmentVariableEditor component.
 */
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { EnvironmentVariableEditor } from './EnvironmentVariableEditor';

describe('EnvironmentVariableEditor', () => {
  describe('rendering', () => {
    it('renders the editor container', () => {
      render(<EnvironmentVariableEditor variables={{}} onChange={() => {}} />);
      expect(screen.getByTestId('env-var-editor')).toBeInTheDocument();
    });

    it('renders empty state when no variables', () => {
      render(<EnvironmentVariableEditor variables={{}} onChange={() => {}} />);
      expect(screen.getByTestId('env-var-empty')).toHaveTextContent(
        'No environment variables defined'
      );
    });

    it('renders existing variables', () => {
      render(
        <EnvironmentVariableEditor
          variables={{ API_KEY: 'secret', NODE_ENV: 'development' }}
          onChange={() => {}}
        />
      );
      expect(screen.getByTestId('env-var-list')).toBeInTheDocument();
      expect(screen.getByTestId('env-var-item-API_KEY')).toBeInTheDocument();
      expect(screen.getByTestId('env-var-item-NODE_ENV')).toBeInTheDocument();
    });

    it('renders add new variable section', () => {
      render(<EnvironmentVariableEditor variables={{}} onChange={() => {}} />);
      expect(screen.getByTestId('env-var-add')).toBeInTheDocument();
      expect(screen.getByTestId('env-var-new-key')).toBeInTheDocument();
      expect(screen.getByTestId('env-var-new-value')).toBeInTheDocument();
      expect(screen.getByTestId('env-var-add-button')).toBeInTheDocument();
    });
  });

  describe('adding variables', () => {
    it('calls onChange when adding a valid variable', () => {
      const onChange = jest.fn();
      render(<EnvironmentVariableEditor variables={{}} onChange={onChange} />);

      fireEvent.change(screen.getByTestId('env-var-new-key'), {
        target: { value: 'NEW_VAR' },
      });
      fireEvent.change(screen.getByTestId('env-var-new-value'), {
        target: { value: 'new_value' },
      });
      fireEvent.click(screen.getByTestId('env-var-add-button'));

      expect(onChange).toHaveBeenCalledWith({ NEW_VAR: 'new_value' });
    });

    it('disables add button when variable name is empty or whitespace', () => {
      render(<EnvironmentVariableEditor variables={{}} onChange={() => {}} />);

      // Button is disabled initially (empty input)
      expect(screen.getByTestId('env-var-add-button')).toBeDisabled();

      // Enter whitespace - button should stay disabled
      fireEvent.change(screen.getByTestId('env-var-new-key'), {
        target: { value: '   ' },
      });
      expect(screen.getByTestId('env-var-add-button')).toBeDisabled();

      // Enter valid name - button should be enabled
      fireEvent.change(screen.getByTestId('env-var-new-key'), {
        target: { value: 'VALID_VAR' },
      });
      expect(screen.getByTestId('env-var-add-button')).not.toBeDisabled();
    });

    it('shows error for duplicate variable name', () => {
      render(
        <EnvironmentVariableEditor
          variables={{ EXISTING: 'value' }}
          onChange={() => {}}
        />
      );

      fireEvent.change(screen.getByTestId('env-var-new-key'), {
        target: { value: 'EXISTING' },
      });
      fireEvent.click(screen.getByTestId('env-var-add-button'));

      expect(screen.getByTestId('env-var-error')).toHaveTextContent(
        'Variable already exists'
      );
    });

    it('shows error for invalid variable name', () => {
      render(<EnvironmentVariableEditor variables={{}} onChange={() => {}} />);

      fireEvent.change(screen.getByTestId('env-var-new-key'), {
        target: { value: '123INVALID' },
      });
      fireEvent.click(screen.getByTestId('env-var-add-button'));

      expect(screen.getByTestId('env-var-error')).toHaveTextContent(
        'Invalid variable name'
      );
    });

    it('clears input fields after successful add', () => {
      const onChange = jest.fn();
      render(<EnvironmentVariableEditor variables={{}} onChange={onChange} />);

      const keyInput = screen.getByTestId('env-var-new-key') as HTMLInputElement;
      const valueInput = screen.getByTestId('env-var-new-value') as HTMLInputElement;

      fireEvent.change(keyInput, { target: { value: 'NEW_VAR' } });
      fireEvent.change(valueInput, { target: { value: 'value' } });
      fireEvent.click(screen.getByTestId('env-var-add-button'));

      expect(keyInput.value).toBe('');
      expect(valueInput.value).toBe('');
    });

    it('allows adding variable with Enter key', () => {
      const onChange = jest.fn();
      render(<EnvironmentVariableEditor variables={{}} onChange={onChange} />);

      fireEvent.change(screen.getByTestId('env-var-new-key'), {
        target: { value: 'NEW_VAR' },
      });
      fireEvent.keyPress(screen.getByTestId('env-var-new-key'), {
        key: 'Enter',
        code: 13,
        charCode: 13,
      });

      expect(onChange).toHaveBeenCalled();
    });
  });

  describe('updating variables', () => {
    it('calls onChange when updating a variable value', () => {
      const onChange = jest.fn();
      render(
        <EnvironmentVariableEditor
          variables={{ MY_VAR: 'old_value' }}
          onChange={onChange}
        />
      );

      const valueInput = screen.getByLabelText('Value for MY_VAR');
      fireEvent.change(valueInput, { target: { value: 'new_value' } });

      expect(onChange).toHaveBeenCalledWith({ MY_VAR: 'new_value' });
    });
  });

  describe('removing variables', () => {
    it('calls onChange when removing a variable', () => {
      const onChange = jest.fn();
      render(
        <EnvironmentVariableEditor
          variables={{ VAR1: 'value1', VAR2: 'value2' }}
          onChange={onChange}
        />
      );

      fireEvent.click(screen.getByLabelText('Remove VAR1'));

      expect(onChange).toHaveBeenCalledWith({ VAR2: 'value2' });
    });
  });

  describe('disabled state', () => {
    it('disables all inputs when disabled', () => {
      render(
        <EnvironmentVariableEditor
          variables={{ MY_VAR: 'value' }}
          onChange={() => {}}
          disabled={true}
        />
      );

      expect(screen.getByLabelText('Value for MY_VAR')).toBeDisabled();
      expect(screen.getByTestId('env-var-new-key')).toBeDisabled();
      expect(screen.getByTestId('env-var-new-value')).toBeDisabled();
      expect(screen.getByTestId('env-var-add-button')).toBeDisabled();
    });

    it('disables remove buttons when disabled', () => {
      render(
        <EnvironmentVariableEditor
          variables={{ MY_VAR: 'value' }}
          onChange={() => {}}
          disabled={true}
        />
      );

      expect(screen.getByLabelText('Remove MY_VAR')).toBeDisabled();
    });
  });
});
