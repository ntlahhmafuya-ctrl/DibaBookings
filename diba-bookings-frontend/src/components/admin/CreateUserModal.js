import { Button, Form, Modal } from "react-bootstrap";

function CreateUserModal({
    show,
    onHide,
    newUser,
    onChange,
    roles,
    onSubmit
}) {
    return (
        <Modal show={show} onHide={onHide}>
            <Form onSubmit={onSubmit}>
                <Modal.Header closeButton>
                    <Modal.Title>Create user</Modal.Title>
                </Modal.Header>
                <Modal.Body>
                    <div className="form-row">
                        <Form.Control
                            placeholder="First name"
                            required
                            value={newUser.firstName}
                            onChange={(event) =>
                                onChange("firstName", event.target.value)
                            }
                        />
                        <Form.Control
                            placeholder="Last name"
                            required
                            value={newUser.lastName}
                            onChange={(event) =>
                                onChange("lastName", event.target.value)
                            }
                        />
                    </div>
                    <Form.Control
                        className="mb-3"
                        type="email"
                        placeholder="Email"
                        required
                        value={newUser.email}
                        onChange={(event) =>
                            onChange("email", event.target.value)
                        }
                    />
                    <Form.Control
                        className="mb-3"
                        type="password"
                        placeholder="Temporary password"
                        required
                        value={newUser.password}
                        onChange={(event) =>
                            onChange("password", event.target.value)
                        }
                    />
                    <Form.Select
                        value={newUser.roleId}
                        onChange={(event) =>
                            onChange("roleId", event.target.value)
                        }
                    >
                        {roles.map((role) => (
                            <option key={role.roleId} value={role.roleId}>
                                {role.roleName}
                            </option>
                        ))}
                    </Form.Select>
                </Modal.Body>
                <Modal.Footer>
                    <Button variant="light" onClick={onHide}>
                        Cancel
                    </Button>
                    <Button type="submit">Create user</Button>
                </Modal.Footer>
            </Form>
        </Modal>
    );
}

export default CreateUserModal;
